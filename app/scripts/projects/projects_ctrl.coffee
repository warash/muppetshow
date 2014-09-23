
angular.module('muppetshowApp')
.controller 'ProjectsCtrl', ($scope, ProjectsSvc) ->
    ProjectsSvc.fetchProjects().then(->
      $scope.projects = ProjectsSvc.active()
      $scope.empname =''
      $scope.$watch('empname', (newV)->
        $scope.projects = ProjectsSvc.filterBy($scope.empname)
      )

      $scope.visiblePersonInfo = false

      $scope.showPersonInfo = (resource)->
        $scope.selectedPerson = resource
        $scope.visiblePersonInfo = true
    )



