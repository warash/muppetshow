using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMService.Model
{
    public class crm_Project
    {
        public Guid ProjectId
        {
            get;
            set;
        }

        public string ProjectName
        {
            get;
            set;
        }

        public string ProjectCode
        {
            get;
            set;
        }

        public string ClientName
        {
            get;
            set;
        }

        public int? ProjectStageId
        {
            get;
            set;
        }
    }
}
