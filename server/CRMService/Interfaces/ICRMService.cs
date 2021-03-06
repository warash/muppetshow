﻿using System;
using System.Collections.Generic;
using System.Linq;
//using CRMService.Model;
//using Infusion.CRMService.Model;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using System.ServiceModel.Web;
using CRMService.Model;
using System.Xml.Serialization;

namespace CRMService.Interfaces
{
    [ServiceContract]
    public interface ICRMService
    {
        [OperationContract]
        [WebGet( ResponseFormat = WebMessageFormat.Json, UriTemplate = "/projectParticipations", BodyStyle = WebMessageBodyStyle.Bare )]
        [AspNetCacheProfile( "ProjectParticipationsCache" )]
        List<ProjectParticipation> GetProjectParticipation();
    }
}