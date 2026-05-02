using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.State.Interfaces
{
    public interface ITableStateDto
    {
       public interface ITableStateDtoProvider
    {
        
        TableStateDto BuildTableStateDto(Table table, string viewerPlayerId);
    }
    }
}