
angular.module('muppetshowApp')
.controller 'ProjectsCtrl', ($scope, ProjectsSvc) ->
    ProjectsSvc.fetchProjects().then(->
      $scope.projects = ProjectsSvc.active()
      $scope.offices = ProjectsSvc.offices()
      $scope.search =
        fraze: ''


      $scope.$watch('search.fraze', (fraze)->
        console.log(fraze)
      )
    )



