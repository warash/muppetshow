
angular.module('muppetshowApp')
  .factory 'ProjectsSvc', ($http, $q) ->
    class ProjectSvc
      all:->
        this.projects
      active:->
        _.where(this.projects, ProjectStage: 'In Progress')

      fetchProjects : ->
        reutrn $q.defer().resolve(this.projects) if this.projects
        $http.get('data/projects.json').then((resp)=>
          this.projects = resp.data )



    new ProjectSvc



