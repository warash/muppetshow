
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
        isOfficeSelected = $scope.selectedOffices.length != 0
        $scope.projects.each((p)->
          allocationMatch = p.Allocations.any((a)->
            match =  a.FirstName.toLowerCase().indexOf(fraze) > -1
            match ||= a.LastName.toLowerCase().indexOf(fraze) > -1
            match ||= (a.FirstName + ' ' + a.LastName).toLowerCase().indexOf(fraze) > -1
            a.selected = match

            officeMatch = $.inArray(a.Office, $scope.selectedOffices) > -1

            return match and (!isOfficeSelected or officeMatch)
          )
          projMatch = p.Name.toLowerCase().indexOf(fraze) > -1

          if isOfficeSelected
            p.visible = allocationMatch
          else
            p.visible = projMatch or allocationMatch
        )

      $scope.visiblePersonInfo = false

      $scope.showPersonInfo = (resource)->
        $scope.selectedPerson = resource
        $scope.visiblePersonInfo = true
        $scope.selLogin = resource.Login

    )



