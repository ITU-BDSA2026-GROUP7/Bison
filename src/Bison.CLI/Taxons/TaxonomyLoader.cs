using System.Reflection;

public static class TaxonomyLoader{
    
    public static TaxonomyRepository Load(){

        var repository = new TaxonomyRepository();

        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream("Bison.CLI.Taxons.joined.csv");

        using var reader = new StreamReader(stream!);

        reader.ReadLine();

        while (!reader.EndOfStream){
            var line = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(line)){
                continue;
            }

            var columns = line.Split(',');

            var taxon = new Taxon{
                TaxonId = columns[0],
                ParentTaxonId = columns[1],
                Rank = columns[4],
                ScientificName = columns[5],
                VernacularName = columns.Length > 9 ? columns[9] : null
            };

            repository.Add(taxon);

        }

        return repository;
    }

}