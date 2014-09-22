'use strict'

###*
 # @ngdoc function
 # @name muppetshowApp.controller:MainCtrl
 # @description
 # # MainCtrl
 # Controller of the muppetshowApp
###
angular.module('muppetshowApp')
  .controller 'MainCtrl', ($scope) ->
    $scope.awesomeThings = [
      'HTML5 Boilerplate'
      'AngularJS'
      'Karma'
    ]
