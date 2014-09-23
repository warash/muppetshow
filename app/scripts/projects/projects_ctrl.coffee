
angular.module('muppetshowApp')
.controller 'ProjectsCtrl', ($scope, ProjectsSvc) ->
    ProjectsSvc.fetchProjects().then(->
      $scope.projects = ProjectsSvc.active()
      $scope.empname =''
      $scope.$watch('empname', (newV)->
        $scope.projects = ProjectsSvc.filterBy($scope.empname)
      )
    )



