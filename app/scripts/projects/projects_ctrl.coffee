
angular.module('muppetshowApp')
.controller 'ProjectsCtrl', ($scope, ProjectsSvc, $activityIndicator) ->
    $activityIndicator.startAnimating()
    ProjectsSvc.fetchProjects().then(->
      $activityIndicator.stopAnimating()
      $scope.projects = ProjectsSvc.active()
      $scope.offices = ProjectsSvc.offices()
      $scope.search =
        fraze: ''


      $scope.$watch('search.fraze', (fraze)->
        $scope.projects.each((p)->
          allocationMatch = p.Allocations.any((a)->
            match =  a.FirstName.toLowerCase().indexOf(fraze) > -1
            match ||= a.LastName.toLowerCase().indexOf(fraze) > -1
            match ||= (a.FirstName + ' ' + a.LastName).toLowerCase().indexOf(fraze) > -1
            a.selected = match

            return match
          )
          projMatch = p.Name.toLowerCase().indexOf(fraze) > -1
          p.visible = projMatch or allocationMatch
        )
      )
    )



