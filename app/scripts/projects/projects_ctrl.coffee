
angular.module('muppetshowApp')
.controller 'ProjectsCtrl', ($scope, ProjectsSvc, $activityIndicator) ->
    $activityIndicator.startAnimating()
    ProjectsSvc.fetchProjects().then(->
      $activityIndicator.stopAnimating()
      $scope.projects = ProjectsSvc.active()
      $scope.offices = ProjectsSvc.offices()
      $scope.selectedOffices = []
      $scope.search =
        fraze: ''

      $scope.includeOffice = (office)->
        i =  $.inArray(office, $scope.selectedOffices)
        if i > -1
          $scope.selectedOffices.splice(i, 1)
        else
          $scope.selectedOffices.push(office)

        $scope.onFilter($scope.search.fraze)

      $scope.$watch('search.fraze', (fraze)->
        $scope.onFilter(fraze)
      )

      $scope.onFilter = (fraze)->
        fraze = fraze.toLowerCase()
        $scope.visiblePersonInfo = false
        isOfficeSelected = $scope.selectedOffices.length != 0
        $scope.projects.each((p)->
          allocationMatch = p.Allocations.filter((a)->
            match =  a.FirstName.toLowerCase().indexOf(fraze) > -1
            match ||= a.LastName.toLowerCase().indexOf(fraze) > -1
            match ||= (a.FirstName + ' ' + a.LastName).toLowerCase().indexOf(fraze) > -1
            a.selected = fraze?.length > 0 and match
            if a.selected
              $scope.showPersonInfo(a)

            return match
          )

          officeMatch = !isOfficeSelected or p.Allocations.filter((a)->
              return $.inArray(a.Office, $scope.selectedOffices) > -1)?.length > 0

          projMatch = p.Name.toLowerCase().indexOf(fraze) > -1

          p.visible = officeMatch and (projMatch or allocationMatch?.length)
          if fraze?.length > 0 and allocationMatch?.length > 0
            $scope.collapseAll()
            $scope.$watch('isCollapse', -> p.isCollapse = false)
        )

      $scope.visiblePersonInfo = false

      $scope.showPersonInfo = (resource)->
        $scope.selectedPerson = resource
        $scope.visiblePersonInfo = true
        $scope.selLogin = resource.EmployeeId
        resource.projects =  new Array
        ProjectsSvc.all().filter((p)->
          wasParticipating  = p.Allocations.any((a)->a.EmployeeId == resource.EmployeeId)
          resource.projects.push(p) if wasParticipating
        )

      $scope.collapse = (resource)->
        resource.isCollapse = !resource.isCollapse

      $scope.collapseAll = ()->
        $scope.projects.each((item)->
          item.isCollapse = true)

      $scope.collapseAll()
    )



