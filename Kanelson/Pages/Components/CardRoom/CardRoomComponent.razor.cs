using Microsoft.AspNetCore.Components;

namespace Kanelson.Pages.Components.CardRoom;

public partial class CardRoomComponent : ComponentBase
{
    [Parameter] 
    public string RoomName { get; set; } = null!;
     
    [Parameter]
    public string Id { get; set; } = null!;

    [Parameter]
    public string HostName { get; set; } = null!;
    
    [Parameter]
    public bool RoomOwner { get; set; }
    
    [Parameter]
    public EventCallback EnterRoomClick { get; set; }
    
    [Parameter]
    public EventCallback DeleteRoomClick { get; set; }
    
    
    private async Task HandleEnterRoomClick()
    {
        await EnterRoomClick.InvokeAsync();
    }
    
    private async Task HandleDeleteRoomClick()
    {
        await DeleteRoomClick.InvokeAsync();
    }
}