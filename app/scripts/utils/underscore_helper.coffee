
methods = [
  "each"
  "map"
  "where"
  "first"
  "reduce"
  "reduceRight"
  "detect"
  "select"
  "unique"
  "reject"
  "all"
  "any"
  "include"
  "invoke"
  "pluck"
  "max"
  "min"
  "sortBy"
  "sortedIndex"
  "toArray"
  "size"
  "first"
  "rest"
  "last"
  "without"
  "indexOf"
  "lastIndexOf"
  "isEmpty"
  "groupBy"
  "countBy"
  "find"
  "reject"
  "findWhere"
  "flatten"
  "difference"
  "compact"
  "contains"
  "some"
  "sample"
  "shuffle"
]


# Mix in each method as a proxy.
_.each methods, (method) ->
  Array::[method] = ->
    _[method].apply _, [this].concat(_.toArray(arguments))




