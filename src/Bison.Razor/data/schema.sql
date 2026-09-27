drop table if exists user;
create table user (
  user_id integer primary key autoincrement,
  username string not null,
  email string not null
);

drop table if exists observation;
create table observation (
  observation_id integer primary key autoincrement,
  author_id integer not null,
  text string not null,
  pub_date integer
);

drop table if exists comments;
create table comments (
  comment_id integer primary key autoincrement,
  observation_id integer,
  author_id integer,
  text string not null,
  pub_date integer
);

drop table if exists proposals;
create table proposals (
  proposal_id integer primary key autoincrement,
  observation_id integer,
  author_id integer,
  taxon_id string not null,
  text string not null,
  pub_date integer
);