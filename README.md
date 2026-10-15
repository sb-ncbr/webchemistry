# Webchemistry

Webchemistry is a set of tools and efficient data structures developed in C# by David Sehnal at co. at Masaryk University, Brno Czech Republic.

The toolkit employs and implements algorithms for comprehensive detection, analysis, and validation of 3D molecular patterns mainly coming from the [Protein Data Bank](https://www.wwpdb.org/) and supports state of the art [mmCIF format](https://en.wikipedia.org/wiki/Macromolecular_Crystallographic_Information_File).

This repository maintains two of the tools, both built as .NET 10 applications:

* **MOLE 2.5** (`mole2`) - location and characterization of channels, tunnels and pores in biomacromolecules.
* **PatternQuery** (`WebChemistry.Queries.Service`) - detection of structural patterns in biomacromolecular structures.

The other Webchemistry software (the Webchemistry web and platform, MotiveValidator, AtomicChargeCalculator, SiteBinder) targets .NET Framework and is no longer maintained. Its last version is available under the `legacy-netframework` tag.

## Building

Using Docker:

```
docker build -f Dockerfile.mole2 -t mole2 .
docker build -f Dockerfile.patternquery -t patternquery .
```

Without Docker, using the .NET 10 SDK:

```
dotnet publish AppsAndServices/Tunnels/WebChemistry.Tunnels.Server/WebChemistry.Tunnels.Server.csproj -c Release -o out/mole2
dotnet publish AppsAndServices/Queries/Service/WebChemistry.Queries.Service/WebChemistry.Queries.Service.csproj -c Release -o out/patternquery
```

## Running

MOLE 2.5 reads its input and parameters from an XML file, see `mole2 --help`. To run the included example:

```
docker run -u root -v ./MoleExample:/data mole2 /data/test.xml
```

The results are written to `MoleExample/test_output`. The container runs as a non-root user by default, `-u root` allows it to write into the mounted folder.

PatternQuery reads the queries from a JSON configuration file, see `WebChemistry.Queries.Service --help` for its format:

```
docker run -u root -v ./work:/work patternquery /work/results /work/config.json
```

## Web services

MOLE is available as a web service at [MOLEonline](https://moleonline.cz/) and PatternQuery at [PatternQuery](https://webchem.ncbr.muni.cz/Platform/PatternQuery/).

## How to cite
Should you find this toolkit useful please cite one of the publications based on what you used:

### Detection - PatternQuery
* Sehnal, D., Pravda, L., Ionescu C.-M., Svobodová Vařeková, R. and Koča, J. (2015) [PatternQuery: web application for fast detection of biomacromolecular structural patterns in the entire Protein Data Bank](https://doi.org/10.1093/nar/gkv561). Nucleic Acids Res., 43, W383–W388.

### Analysis - MOLE
* Sehnal, D., Vařeková, R.S., Berka, K., Pravda, L., Navrátilová, V., Banáš, P., Ionescu, C.M., Otyepka, M. and Koča, J. (2013) [MOLE 2.0: Advanced approach for analysis of biomacromolecular channels](https://doi.org/10.1186/1758-2946-5-39). J. Cheminform., 5, 39.

### Web service - MOLEonline
* Raček, T., Vel’ký, D., Bučeková, G., Schindler, O., Hutařová Vařeková, I., Špačková, A., Bazgier, V., Berka, K. and Svobodová, R. (2025) [MOLEonline: a web-based tool for analysing channels, tunnels, and pores (2025 update)](https://doi.org/10.1093/bioinformatics/btaf486). Bioinformatics, 41(9), btaf486.

* Pravda, L., Sehnal, D., Toušek, D., Navrátilová, V., Bazgier, V., Berka, K., Svobodová Vařeková, R., Koča, J. and Otyepka, M. (2018) [MOLEonline: a web-based tool for analyzing channels, tunnels and pores (2018 update)](https://doi.org/10.1093/nar/gky309). Nucleic Acids Res., 46, W368–W373.
