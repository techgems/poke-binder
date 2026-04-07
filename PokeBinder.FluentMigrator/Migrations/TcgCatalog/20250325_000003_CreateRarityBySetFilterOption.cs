using FluentMigrator;

namespace PokeBinder.FluentMigrator.Migrations.TcgCatalog;

[Migration(20250325_000003)]
[Tags(DatabaseTags.TcgCatalog)]
public class CreateRarityBySetFilterOption : Migration
{
    public override void Up()
    {
        Create.Table("rarityBySetFilterOption")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("setId").AsInt32().NotNullable()
                .ForeignKey("FK_rarityBySetFilterOption_sets", "sets", "id")
            .WithColumn("rarity").AsString(127).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("rarityBySetFilterOption");
    }

}
