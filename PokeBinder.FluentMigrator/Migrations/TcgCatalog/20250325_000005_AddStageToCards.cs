using FluentMigrator;

namespace PokeBinder.FluentMigrator.Migrations.TcgCatalog;

[Migration(20250325_000005)]
[Tags(DatabaseTags.TcgCatalog)]
public class AddStageToCards : Migration
{
    public override void Up()
    {
        Alter.Table("cards")
            .AddColumn("stage").AsString(63).Nullable();
    }

    public override void Down()
    {
        Delete.Column("stage").FromTable("cards");
    }
}
