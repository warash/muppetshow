using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CRMService.Helpers;
using CRMService.Interfaces;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using NLog;
using CRMService.Model;
using System.ServiceModel.Activation;
using System.ServiceModel;
using System.Xml.Serialization;
using System.ServiceModel.Web;

namespace CRMService
{
    [ServiceBehavior( ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single, AddressFilterMode = AddressFilterMode.Any )]
    [AspNetCompatibilityRequirements( RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed )]
    public class CRMService : ICRMService
    {
        #region Members
        private ServerConnection.Configuration _serverConfig;
        private Logger _logger = LogManager.GetCurrentClassLogger();
        #endregion Members

        #region Initializations
        private void InitializeConfiguration()
        {
            try
            {
                //_logger.Info("Getting CRM server connection");
                ServerConnection crmServerConnection = new ServerConnection();
                _serverConfig = crmServerConnection.GetServerConfiguration();
                //_logger.Info("Got CRM server connection");
            }
            catch( Exception e )
            {
                _logger.FatalException( "Getting CRM server connection encountered exception", e );
            }
        }
        #endregion Initializations

        #region ServiceMethods
        public List<ProjectParticipation> GetProjectParticipation()
        {
            try
            {
                _logger.Info( "Executing CRM Query" );
                if( _serverConfig == null )
                    InitializeConfiguration();
                using( var _service = ServerConnection.GetOrganizationProxy( _serverConfig ) )
                {
                    var query = new QueryExpression();
                    Entity projectParticipationsEntity = new Entity( "ihr_projectparticipation" );
                    query.EntityName = projectParticipationsEntity.LogicalName;
                    query.ColumnSet = new ColumnSet( "ihr_projectid", "ihr_employeeid", "ihr_projectparticipationid", "ihr_datefrom", "ihr_dateto" );

                    EntityCollection retrieved = _service.RetrieveMultiple( query );
                    List<Entity> entities = retrieved.Entities.ToList();
                    _logger.Info( "CRM Query returned: " + entities.Count + " project participations" );

                    var projectParticipations = new List<ProjectParticipation>();

                    List<crm_Project> projects = GetCrmProjects( _service, "ihr_project", new string[] 
                                { 
                                    "ihr_projectid", 
                                    "ihr_projectcode", 
                                    "ihr_name", 
                                    "ihr_client",
                                    "statecode"
                                } );
                    List<crm_Employee> employees = GetCrmEmployees( _service, "ihr_employee", new string[]
                                {
                                    "ihr_employeeid",
                                    "ihr_firstname",
                                    "ihr_lastname",
                                    "ihr_enddate",
                                    "ihr_managerid",
                                    "ihr_office",
                                    "emailaddress"
                                });

                    var offices = GetOptionSet( ihr_employee.EntityLogicalName, "ihr_Office", _service );
                    var projStages = GetOptionSet( ihr_project.EntityLogicalName, "statecode", _service );

                    foreach( var item in entities )
                    {
                        crm_ProjectParticipation crm_projectParticipation = CreateProjectParticipation( item );
                        crm_Project project = projects.FirstOrDefault( p => p.ProjectId == crm_projectParticipation.ProjectId );
                        crm_Employee employee = employees.FirstOrDefault( p => p.EmployeeId == crm_projectParticipation.EmployeeId );
                        crm_Employee manager = employee.ManagerId != Guid.Empty ? employees.FirstOrDefault( p => p.EmployeeId == employee.ManagerId ) : null;

                        var projectParticipation = projectParticipations.FirstOrDefault( p => p.ProjectId == project.ProjectId );
                        if( projectParticipation != null && ( !employee.EndDate.HasValue || employee.EndDate >= DateTime.Now ) )
                        {
                            projectParticipation.Allocations.Add( CreateEmployee( crm_projectParticipation, employee, manager, offices ) );
                        }
                        else
                        {
                            IList<Employee> emplList = new List<Employee>();
                            if( ( !employee.EndDate.HasValue || employee.EndDate >= DateTime.Now ) )
                            {
                                emplList.Add( CreateEmployee( crm_projectParticipation, employee, manager, offices ) );
                            }

                            projectParticipations.Add( new ProjectParticipation()
                            {
                                Name = project.ProjectName,
                                Code = project.ProjectCode,
                                ProjectParticipationId = crm_projectParticipation.ProjectParticipationId,
                                ProjectStage = project.ProjectStageId != -1 ? projStages.FirstOrDefault( p => p.Value == project.ProjectStageId ).Text : string.Empty,
                                Allocations = emplList,
                                ProjectId = project.ProjectId
                            } );
                        }
                    }

                    _logger.Info( "CRM Query finished" );
                    return projectParticipations;
                }
            }
            catch( Exception e )
            {
                _logger.FatalException( "CRM Query failed", e );
                throw;
            }
        }
        #endregion ServiceMethods

        #region Methods
        private List<crm_Project> GetCrmProjects( OrganizationServiceProxy _service, string entityName, string[] columns )
        {
            List<Entity> results = GetEntities( _service, entityName, columns );
            List<crm_Project> projects = new List<crm_Project>();

            try
            {
                foreach( var item in results )
                {
                    crm_Project project = new crm_Project()
                    {
                        ProjectId = item.Attributes.Contains( "ihr_projectid" ) ? (Guid)item.Attributes["ihr_projectid"] : Guid.Empty,
                        ProjectCode = item.Attributes.Contains( "ihr_projectcode" ) ? (string)item.Attributes["ihr_projectcode"] : string.Empty,
                        ProjectName = item.Attributes.Contains( "ihr_name" ) ? (string)item.Attributes["ihr_name"] : string.Empty,
                        ClientName = item.Attributes.Contains( "ihr_client" ) ? (string)item.Attributes["ihr_client"] : string.Empty,
                        ProjectStageId = item.Attributes.Contains( "statecode" ) ? ( (OptionSetValue)item.Attributes["statecode"] ).Value : -1
                    };
                    projects.Add( project );
                }
            }
            catch( Exception ex )
            {
                throw new NotSupportedException( ex.Message );
            }
            return projects;
        }

        private List<crm_Employee> GetCrmEmployees( OrganizationServiceProxy _service, string entityName, string[] columns )
        {
            List<Entity> results = GetEntities( _service, entityName, columns );
            List<crm_Employee> employees = new List<crm_Employee>();
            try
            {
                foreach( var item in results )
                {
                    crm_Employee employee = new crm_Employee()
                    {
                        EmployeeId = item.Attributes.Contains( "ihr_employeeid" ) ? (Guid)item.Attributes["ihr_employeeid"] : Guid.Empty,
                        FirstName = item.Attributes.Contains( "ihr_firstname" ) ? (string)item.Attributes["ihr_firstname"] : string.Empty,
                        LastName = item.Attributes.Contains( "ihr_lastname" ) ? (string)item.Attributes["ihr_lastname"] : string.Empty,
                        EndDate = item.Attributes.Contains( "ihr_enddate" ) ? (DateTime?)item.Attributes["ihr_enddate"] : null,
                        ManagerId = item.Attributes.Contains( "ihr_managerid" ) ? ( (EntityReference)item.Attributes["ihr_managerid"] ).Id : Guid.Empty,
                        OfficeId = item.Attributes.Contains( "ihr_office" ) ? ( (OptionSetValue)item.Attributes["ihr_office"] ).Value : -1,
                        Email = item.Attributes.Contains( "emailaddress" ) ? (string)item.Attributes["emailaddress"] : string.Empty
                    };
                    employees.Add( employee );
                }
            }
            catch( Exception ex )
            {
                throw new NotSupportedException( ex.Message );
            }
            return employees;
        }

        private crm_ProjectParticipation CreateProjectParticipation( Entity item )
        {
            return new crm_ProjectParticipation()
            {
                ProjectId = item.Attributes.Contains( "ihr_projectid" ) ? ((EntityReference)item.Attributes["ihr_projectid"]).Id : Guid.Empty,
                EmployeeId = item.Attributes.Contains( "ihr_employeeid" ) ? ((EntityReference)item.Attributes["ihr_employeeid"]).Id : Guid.Empty,
                ProjectParticipationId = item.Attributes.Contains( "ihr_projectparticipationid" ) ? (Guid)item.Attributes["ihr_projectparticipationid"] : Guid.Empty,
                DateTo = item.Attributes.Contains( "ihr_dateto" ) ? (DateTime?)item.Attributes["ihr_dateto"] : null,
                DateFrom = item.Attributes.Contains( "ihr_datefrom" ) ? (DateTime?)item.Attributes["ihr_datefrom"] : null
            };
        }

        private List<Entity> GetEntities( OrganizationServiceProxy _service, string entityName, string[] columns )
        {
            var queryProjects = new QueryExpression();
            Entity projectsEntity = new Entity( entityName );
            queryProjects.EntityName = projectsEntity.LogicalName;
            queryProjects.ColumnSet = new ColumnSet( columns );

            EntityCollection retrieved = _service.RetrieveMultiple( queryProjects );
            List<Entity> results = retrieved.Entities.ToList();
            return results;
        }

        private Employee CreateEmployee( crm_ProjectParticipation crm_projectParticipation, crm_Employee employee, crm_Employee manager, List<OptionalValue> offices )
        {
            return new Employee()
            {
                EmployeeId = employee.EmployeeId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Manager = manager != null ? string.Format( "{0} {1}", manager.FirstName, manager.LastName ) : string.Empty,
                Office = employee.OfficeId != -1 ? offices.FirstOrDefault( p => p.Value == employee.OfficeId ).Text : "<no assignment>",
                Login = !string.IsNullOrEmpty( employee.Email ) ? employee.Email.Substring( 0, employee.Email.Length - 13 ) : employee.Email,
                StartDate = crm_projectParticipation.DateFrom.HasValue ? crm_projectParticipation.DateFrom.Value.ToShortDateString() : string.Empty,
                EndDate = crm_projectParticipation.DateTo.HasValue ? crm_projectParticipation.DateTo.Value.ToShortDateString() : string.Empty,
            };
        }

        private List<OptionalValue> GetOptionSet( string entityName, string fieldName, IOrganizationService service )
        {
            RetrieveEntityRequest retrieveDetails = new RetrieveEntityRequest();
            retrieveDetails.EntityFilters = EntityFilters.All;
            retrieveDetails.LogicalName = entityName;

            RetrieveEntityResponse retrieveEntityResponseObj = (RetrieveEntityResponse)service.Execute( retrieveDetails );
            EntityMetadata metadata = retrieveEntityResponseObj.EntityMetadata;
            var attribiuteMetadata = metadata.Attributes.FirstOrDefault( attribute => String.Equals( attribute.LogicalName, fieldName, StringComparison.OrdinalIgnoreCase ) );
            if( attribiuteMetadata == null )
                return new List<OptionalValue>();
            EnumAttributeMetadata picklistMetadata = attribiuteMetadata as EnumAttributeMetadata;
            OptionSetMetadata options = picklistMetadata.OptionSet;
            return ( from o in options.Options
                     select new OptionalValue
                     {
                         Value = o.Value,
                         Text = o.Label.UserLocalizedLabel.Label
                     } ).ToList();
        }
        #endregion Methods
    }

    public class OptionalValue
    {
        public int? Value
        {
            get;
            set;
        }

        public string Text
        {
            get;
            set;
        }
    }
}