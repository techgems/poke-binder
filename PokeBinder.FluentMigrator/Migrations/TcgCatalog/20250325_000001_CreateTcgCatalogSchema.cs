using FluentMigrator;

namespace PokeBinder.FluentMigrator.Migrations.TcgCatalog;

[Migration(20250325_000001)]
[Tags(DatabaseTags.TcgCatalog)]
public class CreateTcgCatalogSchema : Migration
{
    public override void Up()
    {
        Create.Table("cardCondition")
            .WithColumn("id").AsInt32().PrimaryKey()
            .WithColumn("justTcgValue").AsString(2).Nullable()
            .WithColumn("fullName").AsString(255).Nullable();

        Create.Table("games")
            .WithColumn("id").AsInt32().PrimaryKey()
            .WithColumn("slug").AsString(255).Nullable()
            .WithColumn("name").AsString(255).Nullable();

        Create.Table("generations")
            .WithColumn("id").AsInt32().PrimaryKey()
            .WithColumn("slug").AsString(127).Nullable()
            .WithColumn("name").AsString(255).Nullable()
            .WithColumn("startDateUnix").AsInt64().Nullable()
            .WithColumn("endDateUnix").AsInt64().Nullable()
            .WithColumn("gameId").AsInt32().Nullable()
                .ForeignKey("FK_generations_games", "games", "id");

        Create.Table("sets")
            .WithColumn("id").AsInt32().PrimaryKey()
            .WithColumn("code").AsString(511).Nullable()
            .WithColumn("name").AsString(511).Nullable()
            .WithColumn("fullName").AsString(511).Nullable()
            .WithColumn("releaseDateUnix").AsInt64().Nullable()
            .WithColumn("imageUrl").AsString(511).Nullable()
            .WithColumn("generationId").AsInt32().Nullable()
                .ForeignKey("FK_sets_generations", "generations", "id")
            .WithColumn("priorityOrder").AsBoolean().WithDefaultValue(false)
            .WithColumn("dateLoadedUnix").AsInt64().Nullable();

        Create.Table("cards")
            .WithColumn("id").AsInt32().PrimaryKey()
            .WithColumn("justTcgId").AsString(255).Nullable()
            .WithColumn("tcgPlayerId").AsInt32().Nullable()
            .WithColumn("setId").AsInt32().Nullable()
                .ForeignKey("FK_cards_sets", "sets", "id")
            .WithColumn("name").AsString(255).Nullable()
            .WithColumn("rarity").AsString(127).Nullable()
            .WithColumn("cardNumber").AsString(63).Nullable()
            .WithColumn("imageUrl").AsString(1023).Nullable()
            .WithColumn("maskImageOneUrl").AsString(1023).Nullable()
            .WithColumn("maskImageTwoUrl").AsString(1023).Nullable()
            .WithColumn("hasImageDownloadAttempt").AsBoolean().WithDefaultValue(false);
    }

    public override void Down()
    {
        Delete.Table("cards");
        Delete.Table("sets");
        Delete.Table("generations");
        Delete.Table("games");
        Delete.Table("cardCondition");
    }
}
