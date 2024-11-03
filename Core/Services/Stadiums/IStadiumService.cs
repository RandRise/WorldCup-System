using Core.DTOs.Stadiums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.Stadiums
{
    public interface IStadiumService
    {
        public List<StadiumDTO> GetStadiums();
        Task AddStadium(StadiumDTO stadium);
        Task UpdateStadium(UpdateStadiumDto stadium);
    }
}
