module TaxonFilter {

    class {:extern} Taxon {
        function {:extern} isSubTaxon(ancester: Taxon): bool
    }

    class {:extern} Observation {
        function {:extern} getTaxon(): Taxon
    }

    function FilterBy(root: Taxon, obs: seq<Observation>): seq<Observation>
    {
        if |obs| == 0 then 
            []
        else if obs[0].getTaxon().isSubTaxon(root) then
            [obs[0]] + FilterBy(root, obs[1..])
        else
            FilterBy(root, obs[1..])
    }
}