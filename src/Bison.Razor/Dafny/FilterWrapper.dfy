module FilterWrapper{

    import TF = TaxonFilter

    method Filter(
        root: TF.Taxon,
        obs: seq<TF.Observation>)
        returns (result: seq<TF.Observation>)
        {
            result := TF.FilterBy(root, obs);
        }
}