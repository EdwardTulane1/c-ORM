

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


It doesn't know its modified based id... 