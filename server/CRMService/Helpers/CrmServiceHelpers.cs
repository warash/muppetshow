﻿// =====================================================================
//  This file is part of the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
// =====================================================================
//<snippetCrmServiceHelper>

// These namespaces are found in the Microsoft.Xrm.Sdk.dll assembly
// located in the SDK\bin folder of the SDK download.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;


namespace CRMService.Helpers
{
    /// <summary>
    ///     Provides server connection information.
    /// </summary>
    public class ServerConnection
    {
        #region Inner classes

        /// <summary>
        ///     Stores Microsoft Dynamics CRM server configuration information.
        /// </summary>
        public class Configuration
        {
            public ClientCredentials Credentials = null;
            public ClientCredentials DeviceCredentials = null;
            public Uri DiscoveryUri;
            public AuthenticationProviderType EndpointType;
            public Uri HomeRealmUri = null;
            public String OrganizationName;
            public Uri OrganizationUri;
            public String ServerAddress;
            public String UserPrincipalName;

            #region internal members of the class

            internal Int16 AuthFailureCount = 0;
            internal IServiceManagement<IOrganizationService> OrganizationServiceManagement;
            internal SecurityTokenResponse OrganizationTokenResponse;

            #endregion

            public override bool Equals(object obj)
            {
                //Check for null and compare run-time types.
                if (obj == null || GetType() != obj.GetType()) return false;

                var c = (Configuration) obj;

                if (!ServerAddress.Equals(c.ServerAddress, StringComparison.InvariantCultureIgnoreCase))
                    return false;
                if (!OrganizationName.Equals(c.OrganizationName, StringComparison.InvariantCultureIgnoreCase))
                    return false;
                if (EndpointType != c.EndpointType)
                    return false;
                if (null != Credentials && null != c.Credentials)
                {
                    if (EndpointType == AuthenticationProviderType.ActiveDirectory)
                    {
                        if (!Credentials.Windows.ClientCredential.Domain.Equals(
                            c.Credentials.Windows.ClientCredential.Domain, StringComparison.InvariantCultureIgnoreCase))
                            return false;
                        if (!Credentials.Windows.ClientCredential.UserName.Equals(
                            c.Credentials.Windows.ClientCredential.UserName, StringComparison.InvariantCultureIgnoreCase))
                            return false;
                    }
                    else if (EndpointType == AuthenticationProviderType.LiveId)
                    {
                        if (!Credentials.UserName.UserName.Equals(c.Credentials.UserName.UserName,
                            StringComparison.InvariantCultureIgnoreCase))
                            return false;
                        if (!DeviceCredentials.UserName.UserName.Equals(
                            c.DeviceCredentials.UserName.UserName, StringComparison.InvariantCultureIgnoreCase))
                            return false;
                        if (!DeviceCredentials.UserName.Password.Equals(
                            c.DeviceCredentials.UserName.Password, StringComparison.InvariantCultureIgnoreCase))
                            return false;
                    }
                    else
                    {
                        if (!Credentials.UserName.UserName.Equals(c.Credentials.UserName.UserName,
                            StringComparison.InvariantCultureIgnoreCase))
                            return false;
                    }
                }
                return true;
            }

            public override int GetHashCode()
            {
                int returnHashCode = ServerAddress.GetHashCode()
                                     ^ OrganizationName.GetHashCode()
                                     ^ EndpointType.GetHashCode();
                if (null != Credentials)
                {
                    if (EndpointType == AuthenticationProviderType.ActiveDirectory)
                        returnHashCode = returnHashCode
                                         ^ Credentials.Windows.ClientCredential.UserName.GetHashCode()
                                         ^ Credentials.Windows.ClientCredential.Domain.GetHashCode();
                    else if (EndpointType == AuthenticationProviderType.LiveId)
                        returnHashCode = returnHashCode
                                         ^ Credentials.UserName.UserName.GetHashCode()
                                         ^ DeviceCredentials.UserName.UserName.GetHashCode()
                                         ^ DeviceCredentials.UserName.Password.GetHashCode();
                    else
                        returnHashCode = returnHashCode
                                         ^ Credentials.UserName.UserName.GetHashCode();
                }
                return returnHashCode;
            }
        }

        #endregion Inner classes

        #region Public properties

        public List<Configuration> configurations = null;

        #endregion Public properties

        #region Private properties

        private Configuration config = new Configuration();

        #endregion Private properties

        #region Static methods

        /// <summary>
        ///     Obtains the organization service proxy.
        ///     This would give a better performance than directly calling GetProxy() generic method
        ///     as it uses cached OrganizationServiceManagement in case it is present.
        /// </summary>
        /// <param name="serverConfiguration">An instance of ServerConnection.Configuration</param>
        /// <returns>An instance of organization service proxy</returns>
        public static OrganizationServiceProxy GetOrganizationProxy(
            Configuration serverConfiguration)
        {
            // If organization service management exists, then use it. 
            // Otherwise generate organization service proxy from scratch.
            if (null != serverConfiguration.OrganizationServiceManagement)
            {
                // Obtain the organization service proxy for the Federated, Microsoft account, and OnlineFederated environments. 
                if (serverConfiguration.EndpointType != AuthenticationProviderType.ActiveDirectory)
                {
                    // get the organization service proxy.
                    return GetProxy<IOrganizationService, OrganizationServiceProxy>(serverConfiguration);
                }
                // Obtain organization service proxy for ActiveDirectory environment 
                // using existing organization service management.
                return new ManagedTokenOrganizationServiceProxy(
                    serverConfiguration.OrganizationServiceManagement,
                    serverConfiguration.Credentials);
            }

            // Obtain the organization service proxy for all type of environments.
            return GetProxy<IOrganizationService, OrganizationServiceProxy>(serverConfiguration);
        }

        #endregion Static methods

        #region Public methods

        /// <summary>
        ///     Obtains the server connection information including the target organization's
        ///     Uri and user logon credentials from the user.
        /// </summary>
        public virtual Configuration GetServerConfiguration()
        {
            var _appSettings = ConfigurationManager.AppSettings;

            config.ServerAddress = _appSettings.Get("CRMServerURL");
            Boolean ssl = _appSettings.Get("CRMSSLEnabled").Equals("true", StringComparison.InvariantCultureIgnoreCase);
            config.OrganizationName = _appSettings.Get("CRMOrganization");

            if (ssl)
                config.DiscoveryUri =
                    new Uri(String.Format("https://{0}/XRMServices/2011/Discovery.svc", config.ServerAddress));
            else
                config.DiscoveryUri =
                    new Uri(String.Format("http://{0}/XRMServices/2011/Discovery.svc", config.ServerAddress));

            // Get the target organization.
            if (ssl)
                config.OrganizationUri =
                    new Uri(String.Format("https://{0}/XRMServices/2011/Organization.svc", config.ServerAddress));
            else
                config.OrganizationUri =
                    new Uri(String.Format("http://{0}/XRMServices/2011/Organization.svc", config.ServerAddress));

            config.Credentials = new ClientCredentials();
            config.Credentials.Windows.ClientCredential = new NetworkCredential(_appSettings.Get("UserName"),
                _appSettings.Get("Password"), _appSettings.Get("UserDomain"));
            config.Credentials.UserName.UserName = _appSettings.Get( "UserName" );
            config.Credentials.UserName.Password = _appSettings.Get( "Password" );
            return config;
        }

        /// <summary>
        ///     Discovers the organizations that the calling user belongs to.
        /// </summary>
        /// <param name="service">A Discovery service proxy instance.</param>
        /// <returns>
        ///     Array containing detailed information on each organization that
        ///     the user belongs to.
        /// </returns>
        public OrganizationDetailCollection DiscoverOrganizations(IDiscoveryService service)
        {
            if (service == null) throw new ArgumentNullException("service");
            var orgRequest = new RetrieveOrganizationsRequest();
            var orgResponse =
                (RetrieveOrganizationsResponse) service.Execute(orgRequest);

            return orgResponse.Details;
        }

        /// <summary>
        ///     Finds a specific organization detail in the array of organization details
        ///     returned from the Discovery service.
        /// </summary>
        /// <param name="orgFriendlyName">The friendly name of the organization to find.</param>
        /// <param name="orgDetails">Array of organization detail object returned from the discovery service.</param>
        /// <returns>Organization details or null if the organization was not found.</returns>
        /// <seealso cref="DiscoveryOrganizations" />
        public OrganizationDetail FindOrganization(string orgFriendlyName,
            OrganizationDetail[] orgDetails)
        {
            if (String.IsNullOrWhiteSpace(orgFriendlyName))
                throw new ArgumentNullException("orgFriendlyName");
            if (orgDetails == null)
                throw new ArgumentNullException("orgDetails");
            OrganizationDetail orgDetail = null;

            foreach (OrganizationDetail detail in orgDetails)
            {
                if (String.Compare(detail.FriendlyName, orgFriendlyName,
                    StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    orgDetail = detail;
                    break;
                }
            }
            return orgDetail;
        }

        /// <summary>
        ///     Reads a server configuration file.
        ///     Read the configuration from disk, if it exists, at C:\Users\YourUserName\AppData\Roaming\CrmServer\Credentials.xml.
        /// </summary>
        /// <returns>Is configuration settings already available on disk.</returns>
        public Boolean ReadConfigurations()
        {
            Boolean isConfigExist = false;

            if (configurations == null)
                configurations = new List<Configuration>();

            if (File.Exists(CrmServiceHelperConstants.ServerCredentialsFile))
            {
                XElement configurationsFromFile =
                    XElement.Load(CrmServiceHelperConstants.ServerCredentialsFile);
                foreach (XElement config in configurationsFromFile.Nodes())
                {
                    var newConfig = new Configuration();
                    XElement serverAddress = config.Element("ServerAddress");
                    if (serverAddress != null)
                        if (!String.IsNullOrEmpty(serverAddress.Value))
                            newConfig.ServerAddress = serverAddress.Value;
                    XElement organizationName = config.Element("OrganizationName");
                    if (organizationName != null)
                        if (!String.IsNullOrEmpty(organizationName.Value))
                            newConfig.OrganizationName = organizationName.Value;
                    XElement discoveryUri = config.Element("DiscoveryUri");
                    if (discoveryUri != null)
                        if (!String.IsNullOrEmpty(discoveryUri.Value))
                            newConfig.DiscoveryUri = new Uri(discoveryUri.Value);
                    XElement organizationUri = config.Element("OrganizationUri");
                    if (organizationUri != null)
                        if (!String.IsNullOrEmpty(organizationUri.Value))
                            newConfig.OrganizationUri = new Uri(organizationUri.Value);
                    XElement homeRealmUri = config.Element("HomeRealmUri");
                    if (homeRealmUri != null)
                        if (!String.IsNullOrEmpty(homeRealmUri.Value))
                            newConfig.HomeRealmUri = new Uri(homeRealmUri.Value);

                    XElement vendpointType = config.Element("EndpointType");
                    if (vendpointType != null)
                        newConfig.EndpointType =
                            RetrieveAuthenticationType(vendpointType.Value);
                    if (config.Element("Credentials").HasElements)
                    {
                        newConfig.Credentials =
                            ParseInCredentials(config.Element("Credentials"),
                                newConfig.EndpointType,
                                newConfig.ServerAddress + ":" + newConfig.OrganizationName + ":" +
                                config.Element("Credentials").Element("UserName").Value);
                    }
                    if (newConfig.EndpointType == AuthenticationProviderType.LiveId)
                    {
                        throw new Exception();
                    }
                    XElement userPrincipalName = config.Element("UserPrincipalName");
                    if (userPrincipalName != null)
                        if (!String.IsNullOrWhiteSpace(userPrincipalName.Value))
                            newConfig.UserPrincipalName = userPrincipalName.Value;
                    configurations.Add(newConfig);
                }
            }

            if (configurations.Count > 0)
                isConfigExist = true;

            return isConfigExist;
        }

        /// <summary>
        ///     Writes all server configurations to a file.
        /// </summary>
        /// <remarks>If the file exists, it is overwritten.</remarks>
        public void SaveConfigurations()
        {
            if (configurations == null)
                throw new NullReferenceException("No server connection configurations were found.");

            var file = new FileInfo(CrmServiceHelperConstants.ServerCredentialsFile);

            // Create directory if it does not exist.
            if (!file.Directory.Exists)
                file.Directory.Create();

            // Replace the file if it exists.
            using (FileStream fs = file.Open(FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = new XmlTextWriter(fs, Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Configurations");
                    writer.WriteFullEndElement();
                    writer.WriteEndDocument();
                }
            }

            foreach (Configuration config in configurations)
                SaveConfiguration(CrmServiceHelperConstants.ServerCredentialsFile, config, true);
        }

        /// <summary>
        ///     Writes a server configuration to a file.
        /// </summary>
        /// <param name="pathname">The file name and system path of the output configuration file.</param>
        /// <param name="config">A server connection configuration.</param>
        /// <param name="append">
        ///     If true, the configuration is appended to the file, otherwise a new file
        ///     is created.
        /// </param>
        public void SaveConfiguration(String pathname, Configuration config, bool append)
        {
            if (String.IsNullOrWhiteSpace(pathname)) throw new ArgumentNullException("pathname");
            if (config == null) throw new ArgumentNullException("config");
            // Target is the key with which associated credentials can be fetched from windows credentials manager.
            String target = config.ServerAddress + ":" + config.OrganizationName;
            if (null != config.Credentials)
            {
                switch (config.EndpointType)
                {
                    case AuthenticationProviderType.ActiveDirectory:
                        target = target + ":" + config.Credentials.Windows.ClientCredential.UserName;
                        break;
                    case AuthenticationProviderType.LiveId:
                    case AuthenticationProviderType.Federation:
                    case AuthenticationProviderType.OnlineFederation:
                        target = target + ":" + config.Credentials.UserName.UserName;
                        break;
                    default:
                        target = String.Empty;
                        break;
                }
            }

            XElement configurationsFromFile = XElement.Load(pathname);
            var newConfig =
                new XElement("Configuration",
                    new XElement("ServerAddress", config.ServerAddress),
                    new XElement("OrganizationName", config.OrganizationName),
                    new XElement("DiscoveryUri",
                        (config.DiscoveryUri != null)
                            ? config.DiscoveryUri.OriginalString
                            : String.Empty),
                    new XElement("OrganizationUri",
                        (config.OrganizationUri != null)
                            ? config.OrganizationUri.OriginalString
                            : String.Empty),
                    new XElement("HomeRealmUri",
                        (config.HomeRealmUri != null)
                            ? config.HomeRealmUri.OriginalString
                            : String.Empty),
                    ParseOutCredentials(config.Credentials, config.EndpointType, target),
                    new XElement("EndpointType", config.EndpointType.ToString()),
                    new XElement("UserPrincipalName",
                        (config.UserPrincipalName != null)
                            ? config.UserPrincipalName
                            : String.Empty)
                    );

            if (append)
            {
                configurationsFromFile.Add(newConfig);
            }
            else
            {
                configurationsFromFile.ReplaceAll(newConfig);
            }

            using (var writer = new XmlTextWriter(pathname, Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                configurationsFromFile.Save(writer);
            }
        }

        /// <summary>
        ///     Obtains the user's logon credentials for the target server.
        /// </summary>
        /// <param name="config">An instance of the Configuration.</param>
        /// <returns>Logon credentials of the user.</returns>
        public static ClientCredentials GetUserLogonCredentials(Configuration config)
        {
            var credentials = new ClientCredentials();
            String userName;
            SecureString password;
            String domain;
            Boolean isCredentialExist = (config.Credentials != null) ? true : false;
            switch (config.EndpointType)
            {
                    // An on-premises Microsoft Dynamics CRM server deployment. 
                case AuthenticationProviderType.ActiveDirectory:
                    // Uses credentials from windows credential manager for earlier saved configuration.
                    if (isCredentialExist && !String.IsNullOrWhiteSpace(config.OrganizationName))
                    {
                        domain = config.Credentials.Windows.ClientCredential.Domain;
                        userName = config.Credentials.Windows.ClientCredential.UserName;
                        if (String.IsNullOrWhiteSpace(config.Credentials.Windows.ClientCredential.Password))
                        {
                            Console.Write("\nEnter domain\\username: ");
                            Console.WriteLine(
                                config.Credentials.Windows.ClientCredential.Domain + "\\"
                                + config.Credentials.Windows.ClientCredential.UserName);

                            Console.Write("       Enter Password: ");
                            password = ReadPassword();
                        }
                        else
                        {
                            password = config.Credentials.Windows.ClientCredential.SecurePassword;
                        }
                    }
                        // Uses default credentials saved in windows credential manager for current organization.
                    else if (!isCredentialExist && !String.IsNullOrWhiteSpace(config.OrganizationName))
                    {
                        return null;
                    }
                        // Prompts users to enter credential for current organization.
                    else
                    {
                        String[] domainAndUserName;
                        do
                        {
                            Console.Write("\nEnter domain\\username: ");
                            domainAndUserName = Console.ReadLine().Split('\\');

                            // If user do not choose to enter user name, 
                            // then try to use default credential from windows credential manager.
                            if (domainAndUserName.Length == 1 && String.IsNullOrWhiteSpace(domainAndUserName[0]))
                            {
                                return null;
                            }
                        } while (domainAndUserName.Length != 2 || String.IsNullOrWhiteSpace(domainAndUserName[0])
                                 || String.IsNullOrWhiteSpace(domainAndUserName[1]));

                        domain = domainAndUserName[0];
                        userName = domainAndUserName[1];

                        Console.Write("       Enter Password: ");
                        password = ReadPassword();
                    }
                    if (null != password)
                    {
                        credentials.Windows.ClientCredential =
                            new NetworkCredential(userName, password, domain);
                    }
                    else
                    {
                        credentials.Windows.ClientCredential = null;
                    }

                    break;
                    // A Microsoft Dynamics CRM Online server deployment. 
                case AuthenticationProviderType.LiveId:
                    // An internet-facing deployment (IFD) of Microsoft Dynamics CRM.          
                case AuthenticationProviderType.Federation:
                    // Managed Identity/Federated Identity users using Microsoft Office 365.
                case AuthenticationProviderType.OnlineFederation:
                    // Use saved credentials.
                    if (isCredentialExist)
                    {
                        userName = config.Credentials.UserName.UserName;
                        if (String.IsNullOrWhiteSpace(config.Credentials.UserName.Password))
                        {
                            Console.Write("\n Enter Username: ");
                            Console.WriteLine(config.Credentials.UserName.UserName);

                            Console.Write(" Enter Password: ");
                            password = ReadPassword();
                        }
                        else
                        {
                            password = ConvertToSecureString(config.Credentials.UserName.Password);
                        }
                    }
                        // For OnlineFederation environments, initially try to authenticate with the current UserPrincipalName
                        // for single sign-on scenario.
                    else if (config.EndpointType == AuthenticationProviderType.OnlineFederation
                             && config.AuthFailureCount == 0
                             && !String.IsNullOrWhiteSpace(UserPrincipal.Current.UserPrincipalName))
                    {
                        config.UserPrincipalName = UserPrincipal.Current.UserPrincipalName;
                        return null;
                    }
                        // Otherwise request username and password.
                    else
                    {
                        config.UserPrincipalName = String.Empty;
                        if (config.EndpointType == AuthenticationProviderType.LiveId)
                            Console.Write("\n Enter Microsoft account: ");
                        else
                            Console.Write("\n Enter Username: ");
                        userName = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(userName))
                        {
                            return null;
                        }

                        Console.Write(" Enter Password: ");
                        password = ReadPassword();
                    }
                    credentials.UserName.UserName = userName;
                    credentials.UserName.Password = ConvertToUnsecureString(password);
                    break;
                default:
                    credentials = null;
                    break;
            }
            return credentials;
        }

        /// <summary>
        ///     Prompts user to enter password in console window
        ///     and capture the entered password into SecureString.
        /// </summary>
        /// <returns>Password stored in a secure string.</returns>
        public static SecureString ReadPassword()
        {
            var ssPassword = new SecureString();

            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key == ConsoleKey.Backspace)
                {
                    if (ssPassword.Length != 0)
                    {
                        ssPassword.RemoveAt(ssPassword.Length - 1);
                        Console.Write("\b \b"); // erase last char
                    }
                }
                else if (info.KeyChar >= ' ') // no control chars
                {
                    ssPassword.AppendChar(info.KeyChar);
                    Console.Write("*");
                }
                info = Console.ReadKey(true);
            }

            Console.WriteLine();
            Console.WriteLine();

            // Lock the secure string password.
            ssPassword.MakeReadOnly();

            return ssPassword;
        }

        /// <summary>
        ///     Generic method to obtain discovery/organization service proxy instance.
        /// </summary>
        /// <typeparam name="TService">
        ///     Set IDiscoveryService or IOrganizationService type
        ///     to request respective service proxy instance.
        /// </typeparam>
        /// <typeparam name="TProxy">
        ///     Set the return type to either DiscoveryServiceProxy
        ///     or OrganizationServiceProxy type based on TService type.
        /// </typeparam>
        /// <param name="currentConfig">An instance of existing Configuration</param>
        /// <returns>
        ///     An instance of TProxy
        ///     i.e. DiscoveryServiceProxy or OrganizationServiceProxy
        /// </returns>
        public static TProxy GetProxy<TService, TProxy>(Configuration currentConfig)
            where TService : class
            where TProxy : ServiceProxy<TService>
        {
            // Check if it is organization service proxy request.
            Boolean isOrgServiceRequest = typeof (TService).Equals(typeof (IOrganizationService));

            // Get appropriate Uri from Configuration.
            Uri serviceUri = isOrgServiceRequest
                ? currentConfig.OrganizationUri
                : currentConfig.DiscoveryUri;

            // Set service management for either organization service Uri or discovery service Uri.
            // For organization service Uri, if service management exists 
            // then use it from cache. Otherwise create new service management for current organization.
            IServiceManagement<TService> serviceManagement =
                (isOrgServiceRequest && null != currentConfig.OrganizationServiceManagement)
                    ? (IServiceManagement<TService>) currentConfig.OrganizationServiceManagement
                    : ServiceConfigurationFactory.CreateManagement<TService>(
                        serviceUri);

            if (isOrgServiceRequest)
            {
                if (currentConfig.OrganizationTokenResponse == null)
                {
                    currentConfig.OrganizationServiceManagement =
                        (IServiceManagement<IOrganizationService>) serviceManagement;
                }
            }
                // Set the EndpointType in the current Configuration object 
                // while adding new configuration using discovery service proxy.
            else
            {
                // Get the EndpointType.
                currentConfig.EndpointType = serviceManagement.AuthenticationType;
                // Get the logon credentials.
                currentConfig.Credentials = GetUserLogonCredentials(currentConfig);
            }

            // Set the credentials.
            var authCredentials = new AuthenticationCredentials();

            // If UserPrincipalName exists, use it. Otherwise, set the logon credentials from the configuration.
            if (!String.IsNullOrWhiteSpace(currentConfig.UserPrincipalName))
            {
                // Single sing-on with the Federated Identity organization using current UserPrinicipalName.
                authCredentials.UserPrincipalName = currentConfig.UserPrincipalName;
            }
            else
            {
                authCredentials.ClientCredentials = currentConfig.Credentials;
            }

            Type classType;

            // Obtain discovery/organization service proxy for Federated,
            // Microsoft account and OnlineFederated environments. 
            if (currentConfig.EndpointType !=
                AuthenticationProviderType.ActiveDirectory)
            {
                if (currentConfig.EndpointType == AuthenticationProviderType.LiveId)
                {
                    authCredentials.SupportingCredentials = new AuthenticationCredentials();
                    authCredentials.SupportingCredentials.ClientCredentials =
                        currentConfig.DeviceCredentials;
                }

                AuthenticationCredentials tokenCredentials =
                    serviceManagement.Authenticate(
                        authCredentials);

                if (isOrgServiceRequest)
                {
                    // Set SecurityTokenResponse for the current organization.
                    currentConfig.OrganizationTokenResponse = tokenCredentials.SecurityTokenResponse;
                    // Set classType to ManagedTokenOrganizationServiceProxy.
                    classType = typeof (ManagedTokenOrganizationServiceProxy);
                }
                else
                {
                    // Set classType to ManagedTokenDiscoveryServiceProxy.
                    classType = typeof (ManagedTokenDiscoveryServiceProxy);
                }

                // Invokes ManagedTokenOrganizationServiceProxy or ManagedTokenDiscoveryServiceProxy 
                // (IServiceManagement<TService>, SecurityTokenResponse) constructor.
                return (TProxy) classType
                    .GetConstructor(new[]
                    {
                        typeof (IServiceManagement<TService>),
                        typeof (SecurityTokenResponse)
                    })
                    .Invoke(new object[]
                    {
                        serviceManagement,
                        tokenCredentials.SecurityTokenResponse
                    });
            }

            // Obtain discovery/organization service proxy for ActiveDirectory environment.
            if (isOrgServiceRequest)
            {
                classType = typeof (ManagedTokenOrganizationServiceProxy);
            }
            else
            {
                classType = typeof (ManagedTokenDiscoveryServiceProxy);
            }

            // Invokes ManagedTokenDiscoveryServiceProxy or ManagedTokenOrganizationServiceProxy 
            // (IServiceManagement<TService>, ClientCredentials) constructor.
            return (TProxy) classType
                .GetConstructor(new[]
                {
                    typeof (IServiceManagement<TService>),
                    typeof (ClientCredentials)
                })
                .Invoke(new object[]
                {
                    serviceManagement,
                    authCredentials.ClientCredentials
                });
        }

        /// <summary>
        ///     Convert SecureString to unsecure string.
        /// </summary>
        /// <param name="securePassword">Pass SecureString for conversion.</param>
        /// <returns>unsecure string</returns>
        public static String ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
                throw new ArgumentNullException("securePassword");

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        /// <summary>
        ///     Convert unsecure string to SecureString.
        /// </summary>
        /// <param name="password">Pass unsecure string for conversion.</param>
        /// <returns>SecureString</returns>
        public static SecureString ConvertToSecureString(string password)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            var securePassword = new SecureString();
            foreach (char c in password)
                securePassword.AppendChar(c);
            securePassword.MakeReadOnly();
            return securePassword;
        }

        #endregion Public methods

        #region Protected methods

        /// <summary>
        ///     Obtains the name and port of the server running the Microsoft Dynamics CRM
        ///     Discovery service.
        /// </summary>
        /// <returns>The server's network name and optional TCP/IP port.</returns>
        protected virtual String GetServerAddress(out bool ssl)
        {
            ssl = false;

            Console.Write("Enter a CRM server name and port [crm.dynamics.com]: ");
            String server = Console.ReadLine();

            if (server.EndsWith(".dynamics.com") || String.IsNullOrWhiteSpace(server))
            {
                ssl = true;
            }
            else
            {
                Console.Write("Is this server configured for Secure Socket Layer (https) (y/n) [n]: ");
                String answer = Console.ReadLine();

                if (answer == "y" || answer == "Y")
                    ssl = true;
            }

            return server;
        }

        /// <summary>
        ///     Is this organization provisioned in Microsoft Office 365?
        /// </summary>
        /// <param name="server">The server's network name.</param>
        protected virtual Boolean GetOrgType(String server)
        {
            Boolean isO365Org = false;
            if (String.IsNullOrWhiteSpace(server))
                return isO365Org;
            if (server.IndexOf('.') == -1)
                return isO365Org;

            Console.Write("Is this organization provisioned in Microsoft Office 365 (y/n) [n]: ");
            String answer = Console.ReadLine();

            if (answer == "y" || answer == "Y")
                isO365Org = true;

            return isO365Org;
        }

        /// <summary>
        ///     Obtains the web address (Uri) of the target organization.
        /// </summary>
        /// <returns>Uri of the organization service or an empty string.</returns>
        protected virtual Uri GetOrganizationAddress()
        {
            using (DiscoveryServiceProxy serviceProxy = GetDiscoveryProxy())
            {
                // Obtain organization information from the Discovery service. 
                if (serviceProxy != null)
                {
                    // Obtain information about the organizations that the system user belongs to.
                    OrganizationDetailCollection orgs = DiscoverOrganizations(serviceProxy);

                    if (orgs.Count > 0)
                    {
                        Console.WriteLine("\nList of organizations that you belong to:");
                        for (int n = 0; n < orgs.Count; n++)
                        {
                            Console.Write("\n({0}) {1} ({2})\t", n + 1, orgs[n].FriendlyName, orgs[n].UrlName);
                        }

                        Console.Write("\n\nSpecify an organization number (1-{0}) [1]: ", orgs.Count);
                        String input = Console.ReadLine();
                        if (input == String.Empty)
                        {
                            input = "1";
                        }
                        int orgNumber;
                        Int32.TryParse(input, out orgNumber);
                        if (orgNumber > 0 && orgNumber <= orgs.Count)
                        {
                            config.OrganizationName = orgs[orgNumber - 1].FriendlyName;
                            // Return the organization Uri.
                            return new Uri(orgs[orgNumber - 1].Endpoints[EndpointType.OrganizationService]);
                        }
                        throw new InvalidOperationException("The specified organization does not exist.");
                    }
                    Console.WriteLine("\nYou do not belong to any organizations on the specified server.");
                    return new Uri(String.Empty);
                }
                throw new InvalidOperationException("An invalid server name was specified.");
            }
        }

        /// <summary>
        ///     Get the discovery service proxy based on existing configuration data.
        ///     Added new way of getting discovery proxy.
        ///     Also preserving old way of getting discovery proxy to support old scenarios.
        /// </summary>
        /// <returns>An instance of DiscoveryServiceProxy</returns>
        private DiscoveryServiceProxy GetDiscoveryProxy()
        {
            try
            {
                // Obtain the discovery service proxy.
                DiscoveryServiceProxy discoveryProxy = GetProxy<IDiscoveryService, DiscoveryServiceProxy>(config);
                // Checking authentication by invoking some SDK methods.
                discoveryProxy.Execute(new RetrieveOrganizationsRequest());
                return discoveryProxy;
            }
            catch (SecurityAccessDeniedException ex)
            {
                // If authentication failed using current UserPrincipalName, 
                // request UserName and Password to try to authenticate using user credentials.
                if (!String.IsNullOrWhiteSpace(config.UserPrincipalName) &&
                    ex.Message.Contains("Access is denied."))
                {
                    config.AuthFailureCount += 1;
                }
                else
                {
                    throw ex;
                }
            }
            // You can also catch other exceptions to handle a specific situation in your code, for example, 
            //      System.ServiceModel.Security.ExpiredSecurityTokenException
            //      System.ServiceModel.Security.MessageSecurityException
            //      System.ServiceModel.Security.SecurityNegotiationException                

            // Second trial to obtain the discovery service proxy in case of single sign-on failure.
            return GetProxy<IDiscoveryService, DiscoveryServiceProxy>(config);
        }

        /// <summary>
        ///     Verify passed strings with the supported AuthenticationProviderType.
        /// </summary>
        /// <param name="authType">String AuthenticationType</param>
        /// <returns>Supported AuthenticatoinProviderType</returns>
        private AuthenticationProviderType RetrieveAuthenticationType(String authType)
        {
            switch (authType)
            {
                case "ActiveDirectory":
                    return AuthenticationProviderType.ActiveDirectory;
                case "LiveId":
                    return AuthenticationProviderType.LiveId;
                case "Federation":
                    return AuthenticationProviderType.Federation;
                case "OnlineFederation":
                    return AuthenticationProviderType.OnlineFederation;
                default:
                    throw new ArgumentException(String.Format("{0} is not a valid authentication type", authType));
            }
        }

        /// <summary>
        ///     Parse credentials from an XML node to required ClientCredentials data type
        ///     based on passed AuthenticationProviderType.
        /// </summary>
        /// <param name="credentials">Credential XML node.</param>
        /// <param name="endpointType">AuthenticationProviderType of the credential.</param>
        /// <param name="target">Target is the key with which associated credentials can be fetched.</param>
        /// <returns>Required ClientCredentials type.</returns>
        private ClientCredentials ParseInCredentials(XElement credentials, AuthenticationProviderType endpointType,
            String target)
        {
            var result = new ClientCredentials();
            if (credentials.HasElements)
            {
                Credential cred = CredentialManager.ReadCredentials(target);
                switch (endpointType)
                {
                    case AuthenticationProviderType.ActiveDirectory:
                        if (null != cred && cred.UserName.Contains("\\"))
                        {
                            String[] domainAndUser = cred.UserName.Split('\\');
                            result.Windows.ClientCredential = new NetworkCredential
                            {
                                UserName = domainAndUser[1],
                                Domain = domainAndUser[0],
                                Password = cred.Password
                            };
                        }
                        else
                        {
                            result.Windows.ClientCredential = new NetworkCredential
                            {
                                UserName = credentials.Element("UserName").Value,
                                Domain = credentials.Element("Domain").Value
                            };
                        }
                        break;
                    case AuthenticationProviderType.LiveId:
                    case AuthenticationProviderType.Federation:
                    case AuthenticationProviderType.OnlineFederation:
                        if (null != cred)
                        {
                            result.UserName.UserName = cred.UserName;
                            result.UserName.Password = cred.Password;
                        }
                        else
                        {
                            result.UserName.UserName = credentials.Element("UserName").Value;
                        }
                        break;
                    default:
                        break;
                }
            }
            else
                return null;

            return result;
        }

        /// <summary>
        ///     Parse ClientCredentials into XML node.
        /// </summary>
        /// <param name="clientCredentials">ClientCredentials type.</param>
        /// <param name="endpointType">AuthenticationProviderType of the credentials.</param>
        /// <param name="target">Target is the key with which associated credentials can be fetched.</param>
        /// <returns>XML node containing credentials data.</returns>
        private XElement ParseOutCredentials(ClientCredentials clientCredentials,
            AuthenticationProviderType endpointType, String target)
        {
            if (clientCredentials != null)
            {
                Credential cred = CredentialManager.ReadCredentials(target);
                switch (endpointType)
                {
                    case AuthenticationProviderType.ActiveDirectory:
                        if (cred == null)
                        {
                            // Add entry in windows credential manager for future use.
                            if (!String.IsNullOrWhiteSpace(clientCredentials.Windows.ClientCredential.Password))
                            {
                                CredentialManager.WriteCredentials(target,
                                    new Credential(clientCredentials.Windows.ClientCredential.Domain + "\\"
                                                   + clientCredentials.Windows.ClientCredential.UserName,
                                        clientCredentials.Windows.ClientCredential.Password),
                                    true);
                            }
                        }
                        else
                        {
                            // Replace if the password has been changed.
                            if (!clientCredentials.Windows.ClientCredential.Password.Equals(cred.Password))
                            {
                                CredentialManager.DeleteCredentials(target, false);
                                CredentialManager.WriteCredentials(target,
                                    new Credential(clientCredentials.Windows.ClientCredential.Domain + "\\"
                                                   + clientCredentials.Windows.ClientCredential.UserName,
                                        clientCredentials.Windows.ClientCredential.Password),
                                    true);
                            }
                        }
                        return new XElement("Credentials",
                            new XElement("UserName", clientCredentials.Windows.ClientCredential.UserName),
                            new XElement("Domain", clientCredentials.Windows.ClientCredential.Domain)
                            );
                    case AuthenticationProviderType.LiveId:
                    case AuthenticationProviderType.Federation:
                    case AuthenticationProviderType.OnlineFederation:
                        if (cred == null)
                        {
                            // Add entry in windows credential manager for future use.
                            if (!String.IsNullOrWhiteSpace(clientCredentials.UserName.Password))
                            {
                                CredentialManager.WriteCredentials(target,
                                    new Credential(clientCredentials.UserName.UserName,
                                        clientCredentials.UserName.Password),
                                    true);
                            }
                        }
                        else
                        {
                            // Replace if the password has been changed.
                            if (!clientCredentials.UserName.Password.Equals(cred.Password))
                            {
                                CredentialManager.DeleteCredentials(target, false);
                                CredentialManager.WriteCredentials(target,
                                    new Credential(clientCredentials.UserName.UserName,
                                        clientCredentials.UserName.Password),
                                    true);
                            }
                        }
                        return new XElement("Credentials",
                            new XElement("UserName", clientCredentials.UserName.UserName)
                            );
                    default:
                        break;
                }
            }

            return new XElement("Credentials", "");
        }

        #endregion Private methods

        #region Private Classes

        /// <summary>
        ///     private static class to store constants required by the CrmServiceHelper class.
        /// </summary>
        private static class CrmServiceHelperConstants
        {
            /// <summary>
            ///     Credentials file path.
            /// </summary>
            public static readonly string ServerCredentialsFile = Path.Combine(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrmServer"),
                "Credentials.xml");
        }

        #endregion
    }

    #region Other Classes

    internal sealed class Credential
    {
        private readonly SecureString _password;
        private readonly SecureString _userName;

        internal Credential(CREDENTIAL_STRUCT cred)
        {
            _userName = ConvertToSecureString(cred.userName);
            var size = (int) cred.credentialBlobSize;
            if (size != 0)
            {
                var bpassword = new byte[size];
                Marshal.Copy(cred.credentialBlob, bpassword, 0, size);
                _password = ConvertToSecureString(Encoding.Unicode.GetString(bpassword));
            }
            else
            {
                _password = ConvertToSecureString(String.Empty);
            }
        }

        public Credential(string userName, string password)
        {
            if (String.IsNullOrWhiteSpace(userName))
                throw new ArgumentNullException("userName");
            if (String.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException("password");

            _userName = ConvertToSecureString(userName);
            _password = ConvertToSecureString(password);
        }

        public string UserName
        {
            get { return ConvertToUnsecureString(_userName); }
        }

        public string Password
        {
            get { return ConvertToUnsecureString(_password); }
        }

        /// <summary>
        ///     This converts a SecureString password to plain text
        /// </summary>
        /// <param name="securePassword">SecureString password</param>
        /// <returns>plain text password</returns>
        private string ConvertToUnsecureString(SecureString secret)
        {
            if (secret == null)
                return string.Empty;

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secret);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        /// <summary>
        ///     This converts a string to SecureString
        /// </summary>
        /// <param name="password">plain text password</param>
        /// <returns>SecureString password</returns>
        private SecureString ConvertToSecureString(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return null;

            var securePassword = new SecureString();
            char[] passwordChars = secret.ToCharArray();
            foreach (char pwdChar in passwordChars)
            {
                securePassword.AppendChar(pwdChar);
            }
            securePassword.MakeReadOnly();
            return securePassword;
        }


        /// <summary>
        ///     This structure maps to the CREDENTIAL structure used by native code. We can use this to marshal our values.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CREDENTIAL_STRUCT
        {
            public UInt32 flags;
            public UInt32 type;
            public string targetName;
            public string comment;
            public FILETIME lastWritten;
            public UInt32 credentialBlobSize;
            public IntPtr credentialBlob;
            public UInt32 persist;
            public UInt32 attributeCount;
            public IntPtr credAttribute;
            public string targetAlias;
            public string userName;
        }
    }

    /// <summary>
    ///     This class exposes methods to read, write and delete user credentials
    /// </summary>
    internal static class CredentialManager
    {
        /// <summary>
        ///     Target Name against which all credentials are stored on the disk.
        /// </summary>
        public const string TargetName = "Microsoft_CRMSDK:";

        /// <summary>
        ///     Cache containing secrets in-memory (used to improve performance and avoid IO operations).
        /// </summary>
        private static readonly Dictionary<string, Credential> credentialCache = new Dictionary<string, Credential>();

        public static Uri GetCredentialTarget(Uri target)
        {
            if (null == target)
                throw new ArgumentNullException("target");
            return new Uri(target.GetLeftPart(UriPartial.Authority));
        }

        public static Credential ReadCredentials(String target)
        {
            Credential cachedCredential;

            // Try to read the username from cache
            if (credentialCache.TryGetValue(TargetName + target, out cachedCredential))
            {
                return cachedCredential;
            }

            Credential credential;
            bool bSuccess = NativeMethods.CredRead(TargetName + target, CRED_TYPE.GENERIC, 0, out credential);
            // No match found.
            if (!bSuccess)
            {
                return null;
            }

            credentialCache[TargetName + target] = credential;
            return credential;
        }

        public static Credential ReadWindowsCredential(Uri target)
        {
            Credential credential;
            bool bSuccess = NativeMethods.CredRead(target.Host, CRED_TYPE.DOMAIN_PASSWORD, 0, out credential);
            if (!bSuccess)
            {
                throw new InvalidOperationException("Unable to read windows credentials for Uri {0}. ErrorCode {1}",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            }
            return credential;
        }

        /// <summary>
        ///     Fetches the credentials.
        /// </summary>
        /// <param name="target">Target is the key with which associated credentials can be fetched</param>
        /// <param name="userCredentials">It is the in parameter which contains the username and password</param>
        /// <param name="allowPhysicalStore">If allowPhysicalStore is true then the credentials are stored on disk</param>
        public static void WriteCredentials(String target, Credential userCredentials, bool allowPhysicalStore)
        {
            if (String.IsNullOrWhiteSpace(target))
                throw new ArgumentNullException("target");
            if (null == userCredentials)
                throw new ArgumentNullException("userCredentials");
            // Cache the username and password in memory
            credentialCache[TargetName + target] = userCredentials;

            // Store the credentials if allowed
            string passwordToStore = allowPhysicalStore ? userCredentials.Password : string.Empty;
            var credential = new Credential.CREDENTIAL_STRUCT();
            try
            {
                credential.targetName = TargetName + target;
                credential.type = (UInt32) CRED_TYPE.GENERIC;
                credential.userName = userCredentials.UserName;
                credential.attributeCount = 0;
                credential.persist = (UInt32) CRED_PERSIST.LOCAL_MACHINE;
                byte[] bpassword = Encoding.Unicode.GetBytes(passwordToStore);
                credential.credentialBlobSize = (UInt32) bpassword.Length;
                credential.credentialBlob = Marshal.AllocCoTaskMem(bpassword.Length);
                Marshal.Copy(bpassword, 0, credential.credentialBlob, bpassword.Length);
                if (!NativeMethods.CredWrite(ref credential, 0))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                if (IntPtr.Zero != credential.credentialBlob)
                    Marshal.FreeCoTaskMem(credential.credentialBlob);
            }
        }

        /// <summary>
        ///     Deletes the credentials.
        /// </summary>
        /// <param name="target">Target is the key with which associated credentials can be fetched</param>
        /// <param name="softDelete">
        ///     If a softDelete is done then credentials are deleted only from memory.
        ///     They are completely removed otherwise.
        /// </param>
        public static void DeleteCredentials(String target, bool softDelete)
        {
            if (String.IsNullOrWhiteSpace(target))
                throw new ArgumentNullException("target");
            if (softDelete)
            {
                // Removes only the password
                try
                {
                    Credential tempCredential = ReadCredentials(target);
                    WriteCredentials(target, new Credential(tempCredential.UserName, String.Empty), true);
                }
                catch (Exception)
                {
                    // Do nothing
                }
            }
            else
            {
                // Removes the entry completely
                NativeMethods.CredDelete(TargetName + target, (int) CRED_TYPE.GENERIC, 0);
                credentialCache.Remove(TargetName + target);
            }
        }

        internal enum CRED_PERSIST : uint
        {
            SESSION = 1,
            LOCAL_MACHINE = 2,
            ENTERPRISE = 3
        }

        private enum CRED_TYPE
        {
            GENERIC = 1,
            DOMAIN_PASSWORD = 2,
            DOMAIN_CERTIFICATE = 3,
            DOMAIN_VISIBLE_PASSWORD = 4,
            MAXIMUM = 5
        }

        private sealed class CredentialMarshaler : ICustomMarshaler
        {
            private static CredentialMarshaler _instance;

            public void CleanUpManagedData(object ManagedObj)
            {
                // Nothing to do since all data can be garbage collected.
            }

            public void CleanUpNativeData(IntPtr pNativeData)
            {
                if (pNativeData == IntPtr.Zero)
                {
                    return;
                }
                NativeMethods.CredFree(pNativeData);
            }

            public int GetNativeDataSize()
            {
                throw new NotImplementedException("The method or operation is not implemented.");
            }

            public IntPtr MarshalManagedToNative(object obj)
            {
                throw new NotImplementedException("Not implemented yet");
            }

            public object MarshalNativeToManaged(IntPtr pNativeData)
            {
                if (pNativeData == IntPtr.Zero)
                {
                    return null;
                }
                return
                    new Credential(
                        (Credential.CREDENTIAL_STRUCT)
                            Marshal.PtrToStructure(pNativeData, typeof (Credential.CREDENTIAL_STRUCT)));
            }


            public static ICustomMarshaler GetInstance(string cookie)
            {
                if (null == _instance)
                    _instance = new CredentialMarshaler();
                return _instance;
            }
        }

        private static class NativeMethods
        {
            [DllImport("advapi32.dll", SetLastError = true,
                EntryPoint = "CredReadW", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CredRead(string target, CRED_TYPE type, int reservedFlag,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof (CredentialMarshaler))] out Credential credential);

            [DllImport("Advapi32.dll", SetLastError = true,
                EntryPoint = "CredWriteW", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CredWrite(ref Credential.CREDENTIAL_STRUCT credential, UInt32 flags);

            [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CredFree(IntPtr cred);

            [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CredDelete(string target, int type, int flags);
        }
    }

    /// <summary>
    ///     Wrapper class for DiscoveryServiceProxy to support auto refresh security token.
    /// </summary>
    internal sealed class ManagedTokenDiscoveryServiceProxy : DiscoveryServiceProxy
    {
        private readonly AutoRefreshSecurityToken<DiscoveryServiceProxy, IDiscoveryService> _proxyManager;

        public ManagedTokenDiscoveryServiceProxy(Uri serviceUri, ClientCredentials userCredentials)
            : base(serviceUri, null, userCredentials, null)
        {
            _proxyManager = new AutoRefreshSecurityToken<DiscoveryServiceProxy, IDiscoveryService>(this);
        }

        public ManagedTokenDiscoveryServiceProxy(IServiceManagement<IDiscoveryService> serviceManagement,
            SecurityTokenResponse securityTokenRes)
            : base(serviceManagement, securityTokenRes)
        {
            _proxyManager = new AutoRefreshSecurityToken<DiscoveryServiceProxy, IDiscoveryService>(this);
        }

        public ManagedTokenDiscoveryServiceProxy(IServiceManagement<IDiscoveryService> serviceManagement,
            ClientCredentials userCredentials)
            : base(serviceManagement, userCredentials)
        {
            _proxyManager = new AutoRefreshSecurityToken<DiscoveryServiceProxy, IDiscoveryService>(this);
        }

        protected override void AuthenticateCore()
        {
            _proxyManager.PrepareCredentials();
            base.AuthenticateCore();
        }

        protected override void ValidateAuthentication()
        {
            _proxyManager.RenewTokenIfRequired();
            base.ValidateAuthentication();
        }
    }

    /// <summary>
    ///     Wrapper class for OrganizationServiceProxy to support auto refresh security token
    /// </summary>
    internal sealed class ManagedTokenOrganizationServiceProxy : OrganizationServiceProxy
    {
        private readonly AutoRefreshSecurityToken<OrganizationServiceProxy, IOrganizationService> _proxyManager;

        public ManagedTokenOrganizationServiceProxy(Uri serviceUri, ClientCredentials userCredentials)
            : base(serviceUri, null, userCredentials, null)
        {
            _proxyManager = new AutoRefreshSecurityToken<OrganizationServiceProxy, IOrganizationService>(this);
        }

        public ManagedTokenOrganizationServiceProxy(IServiceManagement<IOrganizationService> serviceManagement,
            SecurityTokenResponse securityTokenRes)
            : base(serviceManagement, securityTokenRes)
        {
            _proxyManager = new AutoRefreshSecurityToken<OrganizationServiceProxy, IOrganizationService>(this);
        }

        public ManagedTokenOrganizationServiceProxy(IServiceManagement<IOrganizationService> serviceManagement,
            ClientCredentials userCredentials)
            : base(serviceManagement, userCredentials)
        {
            _proxyManager = new AutoRefreshSecurityToken<OrganizationServiceProxy, IOrganizationService>(this);
        }

        protected override void AuthenticateCore()
        {
            _proxyManager.PrepareCredentials();
            base.AuthenticateCore();
        }

        protected override void ValidateAuthentication()
        {
            _proxyManager.RenewTokenIfRequired();
            base.ValidateAuthentication();
        }
    }

    /// <summary>
    ///     Class that wraps acquiring the security token for a service
    /// </summary>
    public sealed class AutoRefreshSecurityToken<TProxy, TService>
        where TProxy : ServiceProxy<TService>
        where TService : class
    {
        private readonly TProxy _proxy;
        private ClientCredentials _deviceCredentials;

        /// <summary>
        ///     Instantiates an instance of the proxy class
        /// </summary>
        /// <param name="proxy">Proxy that will be used to authenticate the user</param>
        public AutoRefreshSecurityToken(TProxy proxy)
        {
            if (null == proxy)
            {
                throw new ArgumentNullException("proxy");
            }

            _proxy = proxy;
        }

        /// <summary>
        ///     Prepares authentication before authen6ticated
        /// </summary>
        public void PrepareCredentials()
        {
            if (null == _proxy.ClientCredentials)
            {
                return;
            }

            switch (_proxy.ServiceConfiguration.AuthenticationType)
            {
                case AuthenticationProviderType.ActiveDirectory:
                    _proxy.ClientCredentials.UserName.UserName = null;
                    _proxy.ClientCredentials.UserName.Password = null;
                    break;
                case AuthenticationProviderType.Federation:
                case AuthenticationProviderType.LiveId:
                    _proxy.ClientCredentials.Windows.ClientCredential = null;
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        ///     Renews the token (if it is near expiration or has expired)
        /// </summary>
        public void RenewTokenIfRequired()
        {
            if (null != _proxy.SecurityTokenResponse &&
                DateTime.UtcNow.AddMinutes(15) >= _proxy.SecurityTokenResponse.Response.Lifetime.Expires)
            {
                try
                {
                    _proxy.Authenticate();
                }
                catch (CommunicationException)
                {
                    if (null == _proxy.SecurityTokenResponse ||
                        DateTime.UtcNow >= _proxy.SecurityTokenResponse.Response.Lifetime.Expires)
                    {
                        throw;
                    }

                    // Ignore the exception 
                }
            }
        }
    }

    #endregion
}

//</snippetCrmServiceHelper>