angular.module('muppetshowApp')
.factory 'ProjectsSvc', ($http, $q) ->
  class ProjectSvc
    all: ->
      @projects
    active: ->
      @projects.where(ProjectStage: 'In Progress')

    fetchProjects: ->
      reutrn $q.defer().resolve(this.projects) if this.projects
      $http.get('data/projects.json').then((resp)=>
        this.projects = resp.data)

  new ProjectSvc



