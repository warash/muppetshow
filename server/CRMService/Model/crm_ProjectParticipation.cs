using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMService.Model
{
    public class crm_ProjectParticipation
    {
        public Guid ProjectParticipationId
        {
            get;
            set;
        }

        public Guid EmployeeId
        {
            get;
            set;
        }

        public Guid ProjectId
        {
            get;
            set;
        }

        public DateTime? DateFrom
        {
            get;
            set;
        }

        public DateTime? DateTo
        {
            get;
            set;
        }
    }
}
