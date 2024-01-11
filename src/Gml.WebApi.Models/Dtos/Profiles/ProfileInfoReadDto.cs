using System.Collections.Generic;
using System.Security.AccessControl;

namespace Gml.WebApi.Models.Dtos.Profiles
{
    public class ProfileInfoReadDto
    {
        public string ProfileName { get; set; }
        public string MinecraftVersion { get; set; }
        public string ClientVersion { get; set; }
        public string Arguments { get; set; }
        public string JavaPath { get; set; }
        public List<LocalFileInfoDto> Files { get; set; }
        public List<LocalFileInfoDto> WhiteListFiles { get; set; }
    }

}
