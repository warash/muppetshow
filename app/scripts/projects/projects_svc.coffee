angular.module('muppetshowApp')
.factory 'ProjectsSvc', ($http, $q) ->
  class ProjectSvc
    all: ->
      @projects
    offices:->
        offices = _.sortBy(_.uniq(_.pluck(_.flatten(_.pluck(this.projects, "Allocations")), "Office")))

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
      $http.get('http://10.0.0.207/PeopleProjectsWebApi/api/values').then((resp)=>
        this.projects = @parse(resp.data))

  new ProjectSvc



