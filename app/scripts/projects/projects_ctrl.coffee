
angular.module('muppetshowApp')
.controller 'ProjectsCtrl', ($scope, $http, $q) ->

  employeesFetched = $http.get('/data/employees.json').then((response)=>
    this.employees = response.data
  )

  projectsFetched = $http.get('/data/projects.json').then((response)=>
    this.projects = response.data
  )

  $q.all([employeesFetched, projectsFetched]).then(=>
    $scope.projects = $scope._(this.projects).map((p)=>
      projEmps =  $scope._(this.employees).where(projectId: p.id)
      p.employees = projEmps
      p
    )
  )
