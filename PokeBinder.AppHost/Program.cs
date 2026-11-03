// The dev loop: everything the app depends on at runtime, composed in the shape prod will have.
// Prod is one EC2 running nginx in front of the app, and CloudFront in front of a public S3 bucket
// for card art: pokebinder-proxy is the EC2's nginx, cdn-nginx is CloudFront, MinIO is S3.

var builder = DistributedApplication.CreateBuilder(args);

const string CardBucket = "card-images";

// The folder the ETL downloads card art into, which the seeder copies into the bucket. Its layout
// is the bucket's: a file's path below it is the object key a card stores.
var cardImagesFolder = builder.Configuration["Seeder:CardImagesFolder"]
    ?? throw new InvalidOperationException("Seeder:CardImagesFolder is not set for the AppHost.");

// Generated on the first run and kept in the AppHost's user secrets, so the MinIO volume and the
// password that opens it stay in step across runs.
var minioUser = builder.AddParameter("minio-user", "pokebinder");
var minioPassword = builder.AddParameter(
    "minio-password",
    new GenerateParameterDefault { MinLength = 20, Special = false },
    secret: true,
    persist: true);

// The local S3. MinIO stopped publishing images of its own; Chainguard's is built from MinIO's
// source and kept current. It only starts as root -- its nonroot user is refused even on /tmp.
var minio = builder.AddContainer("minio", "cgr.dev/chainguard/minio", "latest")
    .WithContainerRuntimeArgs("--user", "0")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", minioUser)
    .WithEnvironment("MINIO_ROOT_PASSWORD", minioPassword)
    .WithHttpEndpoint(targetPort: 9000, name: "api")
    .WithHttpEndpoint(targetPort: 9001, name: "console")
    .WithHttpHealthCheck("/minio/health/live", endpointName: "api")
    .WithVolume("pokebinder-minio-data", "/data");

// One-shot: copies the card art into the bucket and exits. `sync` only sends what the bucket does
// not have yet, so the first run uploads everything and every later one only what the ETL has
// added since. The AWS CLI rather than a MinIO client, because it is the same call against real S3.
builder.AddContainer("seeder", "amazon/aws-cli", "latest")
    .WithBindMount(cardImagesFolder, "/images", isReadOnly: true)
    .WithEnvironment("S3_ENDPOINT", minio.GetEndpoint("api"))
    .WithEnvironment("AWS_ACCESS_KEY_ID", minioUser)
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", minioPassword)
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithEnvironment("CARD_BUCKET", CardBucket)
    .WithEntrypoint("/bin/sh")
    .WithArgs("-c", """
        set -e
        aws --endpoint-url "$S3_ENDPOINT" s3api head-bucket --bucket "$CARD_BUCKET" 2>/dev/null \
          || aws --endpoint-url "$S3_ENDPOINT" s3api create-bucket --bucket "$CARD_BUCKET"
        aws --endpoint-url "$S3_ENDPOINT" s3api put-bucket-policy --bucket "$CARD_BUCKET" --policy \
          "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":\"*\",\"Action\":\"s3:GetObject\",\"Resource\":\"arn:aws:s3:::$CARD_BUCKET/*\"}]}"
        aws --endpoint-url "$S3_ENDPOINT" s3 sync /images "s3://$CARD_BUCKET" --only-show-errors \
          --cache-control "public, max-age=604800"
        echo "Card art is in s3://$CARD_BUCKET."
        """)
    .WithParentRelationship(minio)
    .WaitFor(minio);

var cdn = builder.AddContainer("cdn-nginx", "nginx", "alpine")
    .WithBindMount("../aspire/cdn/default.conf.template", "/etc/nginx/templates/default.conf.template", isReadOnly: true)
    .WithEnvironment("MINIO_ORIGIN", minio.GetEndpoint("api"))
    .WithEnvironment("CARD_BUCKET", CardBucket)
    .WithEnvironment("NGINX_ENVSUBST_FILTER", "^(MINIO_ORIGIN|CARD_BUCKET)$")
    .WithHttpEndpoint(port: 8081, targetPort: 80)
    .WithUrlForEndpoint("http", url => url.Url = "http://cdn.pokebinder.localhost:8081")
    .WaitFor(minio);

// A project rather than the Dockerfile's image, so it stays debuggable from the IDE; the Dockerfile
// is what ships. Its card-art URLs point at the CDN host, as they will in prod.
var app = builder.AddProject<Projects.PokeBinder>("pokebinder-app")
    .WithEnvironment("CardImages__BaseUrl", "http://cdn.pokebinder.localhost:8081")
    // nginx forwards the scheme; without this the app would not believe it.
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
    .WaitFor(cdn);

builder.AddContainer("pokebinder-proxy", "nginx", "alpine")
    .WithBindMount("../aspire/proxy/default.conf.template", "/etc/nginx/templates/default.conf.template", isReadOnly: true)
    .WithEnvironment("APP_ORIGIN", app.GetEndpoint("http"))
    .WithEnvironment("NGINX_ENVSUBST_FILTER", "^APP_ORIGIN$")
    .WithHttpEndpoint(port: 8080, targetPort: 80)
    .WithUrlForEndpoint("http", url => url.Url = "http://pokebinder.localhost:8080")
    .WaitFor(app);

builder.Build().Run();
