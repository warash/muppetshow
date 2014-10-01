using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientTest
{
    class Program
    {
        static void Main( string[] args )
        {
            ProjectParticipations.CRMServiceClient client = new ProjectParticipations.CRMServiceClient();
            var a = client.GetProjectParticipation();
            string b = string.Empty;
        }
    }
}
