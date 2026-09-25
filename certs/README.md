# certs/

`zscaler-root-ca.pem` in this folder is a public root CA certificate — it contains only a public
key, no private key material, and is the same certificate distributed by Zscaler Inc. to every
customer running their standard TLS-inspection product (verified byte-for-byte identical to the
copy published in [github.com/CMSgov/dpc-app](https://github.com/CMSgov/dpc-app/blob/main/Zscaler-Root-CA.pem)).
It's committed so `docker build` works out of the box on a network that routes outbound HTTPS
through Zscaler — without it, `dotnet restore` fails inside the build container with a
certificate-chain error, even though the host machine trusts the same proxy fine.


