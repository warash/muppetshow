using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CRMService.Model
{
    [DataContract]
    public class ProjectParticipation
    {
        [DataMember]
        public Guid ProjectParticipationId
        {
            get;
            set;
        }

        [DataMember]
        public Guid ProjectId
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        public string Code
        {
            get;
            set;
        }

        [DataMember]
        public string ProjectStage
        {
            get;
            set;
        }

        [DataMember]
        public IList<Employee> Allocations
        {
            get;
            set;
        }
    }
}
