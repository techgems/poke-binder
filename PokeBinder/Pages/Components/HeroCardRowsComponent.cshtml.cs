using TechGems.StaticComponents;

namespace PokeBinder.Pages.Components;

public class HeroCardRowsComponent : StaticComponent
{

    public IReadOnlyList<IReadOnlyList<string?>>? RowImages { get; set; }
}
