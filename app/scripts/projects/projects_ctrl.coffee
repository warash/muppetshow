
angular.module('muppetshowApp')
.controller 'ProjectsCtrl', ($scope, ProjectsSvc) ->
    ProjectsSvc.fetchProjects().then(->
      $scope.projects = ProjectsSvc.active()
      $scope.search =
        fraze: 'majewski'

#
#      $scope.$watch('empname', (newV)->
#        $scope.projects = ProjectsSvc.filterBy($scope.empname)
#      )
    )



