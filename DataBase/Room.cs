using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase
{
    public class Room
    {
        public string RoomName { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public List<string> Messages { get; set; } = new List<string>();
    }
}
