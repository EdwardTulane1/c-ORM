

structure

Every object (item in table) is entity
Every entity can be new, modified, deleted. Starts with auto new flag. On 1st save it 0's. Then every change will make the "isModified" light up
And a remove will just light up the flag "isDeleted"

Then the "saveChanges" will make all underlying and relationships savings

Dbset is for a table

The base class in DbContext. To start a project you initialize a class inheriting from DbContext. In there you'll define all the object you'll want to work with..

Then DbContext intialize them for you. It reads all the properties of the class. and finds those who are from type Dbset
Foreach of them. It gets the type of the Dbset (Type of teh table),
Then it created a new instance of list of values of the DbsetType and let it hold it. 


A definition of table (entity)
The xml file contains only the properties with Column or Key attributes. The rest are inner stuff
// make a validation on keyElement. This is how to find is a emtity already exists and replace it instead



TODO:
Make it possible to get an element (With all its related entites)
Validations



A strategic decision - to have event on modify or to live with @saveChanges
https://sampathdissanyake.medium.com/racking-data-changes-in-c-net-c2be9ed333fd


The options to better flow included using setPropert/OnPropertyChangedy on the user side itself, which I tried to avoid in order to make the system more user friendly. It means we don;t track changes in entities




in savechanges related entity's might be changes while their related still don't know about it.

SO - a change doesnt matter. we only mapping unchangeable keys
only thing that matters deleting
when a entity is mapped to a already deleted one - we need to know about it and react to it.

options - many to many - covered by checking no IsDeleted flag on the related entities
many to one
one to one
one to many


should I enforce relations from key to key? should I have 1 key only per entity?

Updating value was hard, because I couldn't know if the related entity was deleted. Maybe it was deleted and wasn't written yet 
So I add to update stuff in order to make sure the related entity is not deleted.
Which means circular dependencies are not supported.


I have to make sure how relations are made. what fields mapped to what field and how its saved in there.
The mapping is between id to id. (keyAttribute)


I've used linq just to connect and read objects from the xml


TODO: 
- implement one to one relation
- when deleting enitity for each of its related entites convert to real entity and delete it
- on delete bahaviour is on the deleted side. not on the related side. makes sense for easier access
- the storage path must be same all over
- the one to one has to be implemented
- in mamy to many how we know whos parent and whos related?


what is doesnt do: 0 thoguht about performance. 
really
like none

I can either do block for using new on existing id or let is set isModified to true. I think 1st approach is better.


oneto one relation is saved only on one side. (makes sense, otherwise it wil be saved twice)


delete orphans once in a while.