using FluentMigrator;

namespace PokeBinder.FluentMigrator.Migrations.TcgCatalog;

[Migration(20250325_000004)]
[Tags(DatabaseTags.TcgCatalog)]
public class CreateCardTypeFilterOption : Migration
{
    public override void Up()
    {
        Create.Table("cardTypeFilterOption")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("name").AsString(63).NotNullable();

        foreach (var name in GetSeedData())
        {
            Insert.IntoTable("cardTypeFilterOption").Row(new { name });
        }
    }

    public override void Down()
    {
        Delete.Table("cardTypeFilterOption");
    }

    private static string[] GetSeedData() =>
    [
        "Grass",
        "Fire",
        "Water",
        "Lightning",
        "Psychic",
        "Fighting",
        "Darkness",
        "Metal",
        "Fairy",
        "Dragon",
        "Colorless",
    ];
}
