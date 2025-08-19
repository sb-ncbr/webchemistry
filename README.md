# Webchemistry

Webchemistry is a set of tools and efficient data structures developed in C# by David Sehnal at co. at Masaryk University, Brno Czech Republic. 

The toolkit employs and implements algorithms for comprehensive detection, analysis, and validation of 3D molecular patterns mainly comming from the [Protein Data Bank](https://www.wwpdb.org/) and supports state of the art [mmCIF format](https://en.wikipedia.org/wiki/Macromolecular_Crystallographic_Information_File).

## Installation

Run install-packages.bat or .ps1 in powershell (its enough to copy/paste it there - there is some silly default no script running policy).

To compile all projects:
* Visual Studio 2017 recommended.
* [Visual Studio Silverlight plugin](https://marketplace.visualstudio.com/items?itemName=RamiAbughazaleh.SilverlightProjectSystem)
* [Silverlight 5 SDK](https://www.microsoft.com/en-us/download/details.aspx?id=39597) (install Silverlight_x64.exe and Silverlight_Developer_x86.exe)
* IIS Express to debug the web.

## Web services

Majority of these tools are available as webservices either in [WebChemistry](https://webchem.ncbr.muni.cz/) or [MOLEonline](https://moleonline.cz/).

## How to cite
Should you find this toolkit useful please cite one of the publications based on what you used:

### Detection - PatternQuery
* Sehnal, D., Pravda, L., Ionescu C.-M., Svobodová Vařeková, R. and Koča, J. (2015) [PatternQuery: web application for fast detection of biomacromolecular structural patterns in the entire Protein Data Bank]((https://dx.doi.org/10.1093/nar/gkv561)). Nucleic Acids Res., 43, W383–W388.

### Validation
* Svobodová Vařeková, R., Jaiswal, D., Sehnal, D., Ionescu, C.-M., Geidl, S., Pravda, L., Horský, V., Wimmerová, M. and Koča, J. (2014) [MotiveValidator: interactive web-based validation of ligand and residue structure in biomolecular complexes](https://dx.doi.org/10.1093/nar/gku426). Nucleic Acids Res., 42, W227–33.

* Sehnal, D., Svobodová Vařeková, R., Pravda, L., Ionescu, C.-M., Geidl, S., Horský, V., Jaiswal, D., Wimmerová, M. and Koča, J. (2015) [ValidatorDB: database of up-to-date validation results for ligands and non-standard residues from the Protein Data Bank](https://dx.doi.org/10.1093/nar/gku1118). Nucleic Acids Res. 43, D369–D375.

### Analysis - MOLE
* Sehnal, D., Vařeková, R.S., Berka, K., Pravda, L., Navrátilová, V., Banáš, P., Ionescu, C.M., Otyepka, M. and Koča, J. (2013) [MOLE 2.0: Advanced approach for analysis of biomacromolecular channels](https://dx.doi.org/10.1186/1758-2946-5-39). J. Cheminform., 5, 39.

* Ionescu,C.-M., Sehnal,D., Falginella,F.L., Pant,P., Pravda,L., Bouchal,T., Vařeková,R.S., Geidl,S. and and Koča,J. (2015) [AtomicChargeCalculator: Interactive Web-based calculation of atomic charges in large biomolecular complexes and drug like molecules](https://dx.doi.org/10.1186/s13321-015-0099-x). J. Cheminform., 7, 50.

* Sehnal, D., Svobodová Vařeková, R., Huber, H. J., Geidl, S., Ionescu, C. M., Wimmerová, M. and Koča, J. (2012) [SiteBinder: An improved approach for comparing multiple protein structural motifs](https://dx.doi.org/10.1021/ci200444d). J. Chem. Inf. Model., 52(2), 343–359.

