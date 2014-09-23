angular.module('muppetshowApp')
.factory 'ProjectsSvc', ($http, $q) ->
  class ProjectSvc
    all: ->
      @projects
    active: ->
      active = @projects.where(ProjectStage: 'In Progress')
      active.each((p)->
        p.Allocations = p.Allocations.filter((a)->
          a.EndDate > new Date()
        )
      )
      active = active.filter((p)-> p.Allocations.length > 0)
      active

    parse:(projects)->
      projects.each((p)->
        p.Allocations.each((a)->
          a.StartDate = Date.parse(a.StartDate)
          a.EndDate = Date.parse(a.EndDate)
        )
      )
    fetchProjects: ->
      reutrn $q.defer().resolve(this.projects) if this.projects
      $http.get('data/projects.json').then((resp)=>
        this.projects = @parse(resp.data))

  new ProjectSvc



