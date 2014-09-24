
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
          allocationMatch = p.Allocations.filter((a)->
            match =  a.FirstName.toLowerCase().indexOf(fraze) > -1
            match ||= a.LastName.toLowerCase().indexOf(fraze) > -1
            match ||= (a.FirstName + ' ' + a.LastName).toLowerCase().indexOf(fraze) > -1
            a.selected = fraze?.length and match

            return match
          )
<<<<<<< HEAD
          $scope.visiblePersonInfo = false
=======
          officeMatch = !isOfficeSelected or p.Allocations.filter((a)->
              return $.inArray(a.Office, $scope.selectedOffices) > -1)?.length > 0
>>>>>>> 564240914179deadf8e703aa51397eb9a026132f

          projMatch = p.Name.toLowerCase().indexOf(fraze) > -1

          p.visible = officeMatch and (projMatch or allocationMatch?.length)
        )

      $scope.visiblePersonInfo = false

      $scope.showPersonInfo = (resource)->
        $scope.selectedPerson = resource
        $scope.visiblePersonInfo = true
        $scope.selLogin = resource.Login
        resource.projects =  new Array
        ProjectsSvc.all().filter((p)->
          wasParticipating  = p.Allocations.any((a)->a.Login == resource.Login)
          resource.projects.push(p) if wasParticipating
        )

    )



