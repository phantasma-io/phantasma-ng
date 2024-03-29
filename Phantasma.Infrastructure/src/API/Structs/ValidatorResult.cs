namespace Phantasma.Infrastructure.API.Structs;

public class ValidatorResult
{
    [APIDescription("Address of validator")]
    public string address { get; set; }

    [APIDescription("Either primary or secondary")]
    public string type { get; set; }
}