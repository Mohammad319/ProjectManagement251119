namespace BlazorMHD.UI.Models;

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string City { get; set; } = "";
    public bool IsActive { get; set; }
}
