using GameService.DTOs;
using GameService.State;

namespace GameService.State.Interfaces
{
    public interface ITableStateDtoProvider
    {
        TableStateDto BuildTableStateDto(Table table, string viewerPlayerId);
    }
}