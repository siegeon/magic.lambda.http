
# Magic Lambda HTTP

[![Build status](https://travis-ci.org/polterguy/magic.lambda.http.svg?master)](https://travis-ci.org/polterguy/magic.lambda.http)

Provides HTTP REST capabilities for [Magic](https://github.com/polterguy/magic). More specifically it provdes 4 slots.

* __[http.get]__ - Returns some resource using the HTTP GET verb towards the specified URL.
* __[http.delete]__ - Deletes some resource using the HTTP DELETE verb.
* __[http.post]__ - Posts some resources to some URL using the HTTP POST verb.
* __[http.put]__ - Puts some resources to some URL using the HTTP PUT verb.

The __[http.put]__ and the __[http.post]__ slots requires you to provide a __[payload]__, which will be pass to the
endpoint as s string. All 4 endpoints can (optionally) take a __[token]__ arguments, which will be transferred as a `Bearer Authorization`
token to the endpoint, in the HTTP Authorization header of your request.

## License

Magic is licensed as Affero GPL. This means that you can only use it to create Open Source solutions.
If this is a problem, you can contact at thomas@gaiasoul.com me to negotiate a proprietary license if
you want to use the framework to build closed source code. This will allow you to use Magic in closed
source projects, in addition to giving you access to Microsoft SQL Server adapters, to _"crudify"_
database tables in MS SQL Server. I also provide professional support for clients that buys a
proprietary enabling license.
