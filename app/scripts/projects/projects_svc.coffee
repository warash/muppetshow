angular.module('muppetshowApp')
.factory 'ProjectsSvc', ($http, $q) ->
  class ProjectSvc
    all: ->
      @projects
    offices:->
        offices = _.sortBy(_.uniq(_.pluck(_.flatten(_.pluck(this.projects, "Allocations")), "Office")))

    active: ->
      active = @projects.where(ProjectStage: 'Active')
      active = JSON.parse(JSON.stringify(active))
      now = new Date
      lastMonthStart = new Date 1900+now.getYear(), now.getMonth()-1, 1
      active.each((p)->
        p.Allocations = p.Allocations.filter((a)->
          a.EndDate > lastMonthStart
        )
      )
      active = active.filter((p)-> p.Allocations.length > 0)

    parse:(projects)->
      projects
      projects.each((p)->
        p.Allocations.each((a)->
          a.StartDate = Date.parse(a.StartDate)
          a.EndDate = Date.parse(a.EndDate)
        )
      )
    fetchProjects: ->
      reutrn $q.defer().resolve(this.projects) if this.projects
      $http.get('http://10.1.70.52/muppetshow/ProjectParticipationsService.svc/projectParticipations').then((resp)=>
        this.projects = @parse(resp.data))

  new ProjectSvc



