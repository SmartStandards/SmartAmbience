
# SmartAmbience

The '**AmbientField**' is the next generation of 'AsyncLocal'.

* It can **survive a context switch**!
* All states can be accessed from a centralized component named '**AmbienceHub**' which offers you a possibility to **create and restore snapshots of this contextual states**.
* It is an indispensable building brick to store binding identifiers when **building up multi tenant** webservices or other multi-hop scenarios where contextual identifiers needs to be 'flowed'
* It has several security features and is designed **enterprise ready**
* It is compatible with our **[UJMW](https://github.com/SmartStandards/UnifiedJsonMessageWrapper) communication library** which can be used to make JSON based webservice calls an a quite easy way. If the frameworks are coupled, the values of the AmbientFields will be **automatically included in the requests and restored on the remote side** WITHOUT the need to be part of any method signature.



see the [changelog.md](./vers/changelog.md)

the package is available on [nuget.org](https://www.nuget.org/packages/SmartAmbience)

Build state: ![](https://dev.azure.com/SmartOpenSource/Smart%20Standards%20(Allgemein)/_apis/build/status/SmartAmbience)

