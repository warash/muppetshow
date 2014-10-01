using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMService.Model
{
    public class crm_Employee
    {
        public Guid EmployeeId
        {
            get;
            set;
        }

        public string FirstName
        {
            get;
            set;
        }

        public string LastName
        {
            get;
            set;
        }

        public Guid? ManagerId
        {
            get;
            set;
        }

        public DateTime? EndDate
        {
            get;
            set;
        }

        public int? OfficeId
        {
            get;
            set;
        }
    }
}
