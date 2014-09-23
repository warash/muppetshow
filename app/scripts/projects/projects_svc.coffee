
angular.module('muppetshowApp')
  .factory 'ProjectsSvc', ($http, $q) ->
    class ProjectSvc
      all:->
        this.projects
      active:->
        active = _.where(this.projects, ProjectStage: 'In Progress')
      offices:->
        offices = _.sortBy(_.uniq(_.pluck(_.flatten(_.pluck(this.projects, "Allocations")), "Office")))



      filterBy:(fraze) ->
        fraze = fraze.toLowerCase()
        items = @active()
        return items unless fraze
        _.filter(items, (p)->
          users = _.filter(p.Allocations, ( (e)->
            e.FirstName.toLowerCase().indexOf(fraze) > -1 or e.LastName.toLowerCase().indexOf(fraze) > -1
          ))
          return users != undefined and users.length != 0
        )
      fetchProjects : ->
        reutrn $q.defer().resolve(this.projects) if this.projects
        $http.get('data/projects.json').then((resp)=>
          this.projects = resp.data )

    new ProjectSvc

angular.module('muppetshowApp').filter "filterProjects", ->
  (items, search) ->
    return items  unless search?.fraze
    fraze = search.fraze.toLowerCase()
    return items unless fraze
    _.filter(items, (p)->
      users = _.filter(p.Allocations, ( (e)->
        e.FirstName.toLowerCase().indexOf(fraze) > -1 or e.LastName.toLowerCase().indexOf(fraze) > -1
      ))
      return users != undefined and users.length != 0
    )



