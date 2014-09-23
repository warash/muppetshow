
angular.module('muppetshowApp')
.controller 'ProjectsCtrl', ($scope, ProjectsSvc) ->
    ProjectsSvc.fetchProjects().then(->
      $scope.projects = ProjectsSvc.active()
      $scope.search =
        fraze: ''


      $scope.$watch('search.fraze', (fraze)->
        console.log(fraze)
      )
    )



