
# Magic Lambda HTTP

[![Build status](https://travis-ci.org/polterguy/magic.lambda.http.svg?master)](https://travis-ci.org/polterguy/magic.lambda.http)

Provides HTTP REST capabilities for [Magic](https://github.com/polterguy/magic). More specifically it provides 4 slots.

* __[http.get]__ - Returns some resource using the HTTP GET verb towards the specified URL.
* __[http.delete]__ - Deletes some resource using the HTTP DELETE verb.
* __[http.post]__ - Posts some resources to some URL using the HTTP POST verb.
* __[http.put]__ - Puts some resources to some URL using the HTTP PUT verb.

The __[http.put]__ and the __[http.post]__ slots requires you to provide a __[payload]__, which will be pass to the
endpoint as a string. All 4 endpoints can (optionally) take a __[token]__ arguments, which will be transferred as
a `Bearer Authorization` token to the endpoint, in the HTTP Authorization header of your request.

Notice, if you want to have more control over your HTTP request, you can also explicitly add your own
**[header]** collection, which will become the HTTP request's headers, where the header name is the name
of the node, and its value is the value of the node. Below is a simple example of retrieving the document
found at the specified URL, using a simple GET HTTP request, without neither a **[token]** nor **[header]**
collection.

```
http.get:"https://google.com"
```

## License

Although most of Magic's source code is Open Source, you will need a license key to use it.
[You can obtain a license key here](https://servergardens.com/buy/).
Notice, 7 days after you put Magic into production, it will stop working, unless you have a valid
license for it.

* [Get licensed](https://servergardens.com/buy/)
