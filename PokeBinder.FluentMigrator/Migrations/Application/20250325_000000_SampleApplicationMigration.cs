using FluentMigrator;

namespace PokeBinder.FluentMigrator.Migrations.Application;

[Migration(20250325_000000)]
[Tags(DatabaseTags.Application)]
public class SampleApplicationMigration : Migration
{
    public override void Up()
    {
        Create.Table("SampleTable")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Name").AsString(256).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);
    }

    public override void Down()
    {
        Delete.Table("SampleTable");
    }
}
