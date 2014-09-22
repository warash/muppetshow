angular
  .module('muppetshowApp', [
    'ngAnimate',
    'ngCookies',
    'ngResource',
    'ngRoute',
    'ngSanitize',
    'ngTouch',
    'angular-underscore'
  ])
  .config ($routeProvider) ->
    $routeProvider
      .when '/',
        templateUrl: 'scripts/projects/projects.html'
        controller: 'ProjectsCtrl'
      .otherwise
        redirectTo: '/'

