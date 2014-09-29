using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CRMService.Model
{
    [DataContract]
    [KnownType( typeof( List<object> ) )]
    public class Employee
    {
        [DataMember]
        public Guid EmployeeId
        {
            get;
            set;
        }

        [DataMember]
        public string FirstName
        {
            get;
            set;
        }

        [DataMember]
        public string LastName
        {
            get;
            set;
        }

        [DataMember]
        public string Manager
        {
            get;
            set;
        }

        [DataMember]
        public string Office
        {
            get;
            set;
        }

        [DataMember]
        public string Login
        {
            get;
            set;
        }

        [DataMember]
        public bool IsActive
        {
            get;
            set;
        }

        [DataMember]
        public string StartDate
        {
            get;
            set;
        }

        [DataMember]
        public string EndDate
        {
            get;
            set;
        }
    }
}
