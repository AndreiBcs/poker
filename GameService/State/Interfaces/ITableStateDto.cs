using GameService.DTOs;

namespace GameService.State.Interfaces
{
    public interface ITableStateDtoProvider
    {
        TableStateDto BuildTableStateDto(Table table, string viewerPlayerId);
    }
}