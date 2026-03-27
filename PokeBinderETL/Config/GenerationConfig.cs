using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.ETL.Config;


public class GenerationConfig
{
    public DateOnly ReleaseDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string BaseDirectory { get; set; } = string.Empty;

    public List<SetConfig> Sets { get; set; } = new List<SetConfig>();
}

public class SetConfig
{
    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public List<string> CsvFiles { get; set; } = new List<string>();

    public DateOnly ReleaseDate { get; set; }

    public bool? PriorityOrder { get; set; }
}
