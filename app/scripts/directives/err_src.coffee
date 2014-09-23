
angular.module("muppetshowApp").directive "errSrc", ->
  link: (scope, element, attrs) ->
    element.bind "error", ->
      attrs.$set "src", attrs.errSrc  unless attrs.src is attrs.errSrc
