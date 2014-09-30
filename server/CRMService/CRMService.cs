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
                    query.EntityName = ihr_projectparticipation.EntityLogicalName;
                    query.ColumnSet = new ColumnSet( true );

                    EntityCollection retrieved = _service.RetrieveMultiple( query );
                    List<Entity> entities = retrieved.Entities.ToList();
                    _logger.Info( "CRM Query returned: " + entities.Count + " project participations" );

                    var projectParticipations = new List<ProjectParticipation>();

                    List<ihr_project> projects = GetProjects( _service );
                    List<ihr_employee> employees = GetEmployees( _service );

                    var offices = GetOptionSet( ihr_employee.EntityLogicalName, "ihr_Office", _service );

                    foreach( var item in entities )
                    {
                        var ent = item.ToEntity<ihr_projectparticipation>();
                        ihr_project project = projects.FirstOrDefault( p => p.ihr_projectId == ent.ihr_ProjectId.Id );
                        ihr_employee employee = employees.FirstOrDefault( p => p.ihr_employeeId == ent.ihr_EmployeeId.Id );
                        ihr_employee manager = employees.FirstOrDefault( p => p.ihr_ManagerId == employee.ihr_ManagerId );

                        if( projectParticipations.Any( p => p.ProjectId == project.ihr_projectId ) )
                        {
                            var existing = projectParticipations.FirstOrDefault( p => p.ProjectId == project.ihr_projectId );
                            existing.Allocations.Add( CreateEmployee( ent, employee, manager, offices ) );
                        }
                        else
                        {
                            IList<Employee> emplList = new List<Employee>();
                            emplList.Add( CreateEmployee( ent, employee, manager, offices ) );

                            projectParticipations.Add( new ProjectParticipation()
                            {
                                Name = project.ihr_name,
                                Code = project.ihr_ProjectCode,
                                ProjectParticipationId = (Guid)item["ihr_projectparticipationid"],
                                ProjectStage = project.statecode != null ? project.statecode.Value.ToString() : "<no data>",
                                Allocations = emplList,
                                ProjectId = project.ihr_projectId.Value
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

        private Employee CreateEmployee( ihr_projectparticipation ent, ihr_employee employee, ihr_employee manager, List<OptionalValue> offices )
        {
            return new Employee()
            {
                EmployeeId = employee.ihr_employeeId.Value,
                FirstName = employee.ihr_FirstName,
                LastName = employee.ihr_LastName,
                Manager = manager.ihr_fullname,
                Office = employee.ihr_Office != null ? offices.FirstOrDefault( p => p.Value == employee.ihr_Office.Value ).Text : "<no assignment>",
                Login = !string.IsNullOrEmpty( employee.EmailAddress ) ? employee.EmailAddress.Substring( 0, employee.EmailAddress.Length - 13 ) : employee.EmailAddress,
                IsActive = ent.statecode.Value == ihr_projectparticipationState.Active,
                StartDate = ent.ihr_DateFrom.HasValue ? ent.ihr_DateFrom.Value.ToShortDateString() : string.Empty,
                EndDate = ent.ihr_DateTo.HasValue ? ent.ihr_DateTo.Value.ToShortDateString() : string.Empty
            };
        }

        public List<OptionalValue> GetOptionSet( string entityName, string fieldName, IOrganizationService service )
        {
            RetrieveEntityRequest retrieveDetails = new RetrieveEntityRequest();
            retrieveDetails.EntityFilters = EntityFilters.All;
            retrieveDetails.LogicalName = entityName;

            RetrieveEntityResponse retrieveEntityResponseObj = (RetrieveEntityResponse)service.Execute( retrieveDetails );
            EntityMetadata metadata = retrieveEntityResponseObj.EntityMetadata;
            PicklistAttributeMetadata picklistMetadata = metadata.Attributes.FirstOrDefault( attribute => String.Equals( attribute.LogicalName, fieldName, StringComparison.OrdinalIgnoreCase ) ) as PicklistAttributeMetadata;
            OptionSetMetadata options = picklistMetadata.OptionSet;
            return ( from o in options.Options
                     select new OptionalValue
                     {
                         Value = o.Value,
                         Text = o.Label.UserLocalizedLabel.Label
                     } ).ToList();

        }
        #endregion ServiceMethods

        #region Methods
        private List<ihr_project> GetProjects( OrganizationServiceProxy _service )
        {
            var queryProjects = new QueryExpression();
            queryProjects.EntityName = ihr_project.EntityLogicalName;
            queryProjects.ColumnSet = new ColumnSet( true );

            EntityCollection retrieved = _service.RetrieveMultiple( queryProjects );
            List<Entity> results = retrieved.Entities.ToList();

            List<ihr_project> projects = new List<ihr_project>();

            foreach( var item in results )
            {
                var ent = item.ToEntity<ihr_project>();
                projects.Add( ent );
            }
            return projects;
        }

        private List<ihr_employee> GetEmployees( OrganizationServiceProxy _service )
        {
            var queryEmployees = new QueryExpression();
            queryEmployees.EntityName = ihr_employee.EntityLogicalName;
            queryEmployees.ColumnSet = new ColumnSet( true );

            EntityCollection retrieved = _service.RetrieveMultiple( queryEmployees );
            List<Entity> results = retrieved.Entities.ToList();

            List<ihr_employee> employees = new List<ihr_employee>();

            foreach( var item in results )
            {
                var ent = item.ToEntity<ihr_employee>();
                employees.Add( ent );
            }
            return employees;
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