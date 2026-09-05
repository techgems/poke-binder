-- The grids a binder can be created on. This is a closed list the user picks from, not something
-- they type, so it is seeded here rather than left to the application.
--
-- [name] is the grid, [description] is what a page of it holds. They are separate columns so a
-- picker can show the name large and the blurb under it, and so neither has to be parsed back out
-- of a combined string.
--
-- defaultPages is what that grid is commonly sold with, and only prefills the page count on the
-- create form -- the binder stores its own. The 20-pocket grids are the least standardised, so
-- their default is the most likely of these numbers to want changing.
--
-- OR IGNORE so that re-running against a database that was seeded by hand does not fail on the id
-- or on IX_binderSizes_x_y.
INSERT OR IGNORE INTO [binderSizes] ([id], [name], [description], [x], [y], [defaultPages]) VALUES
    (1, '2x2', '4 cards per page',  2, 2, 40),
    (2, '3x3', '9 cards per page',  3, 3, 40),
    (3, '4x3', '12 cards per page', 4, 3, 52),
    (4, '4x4', '16 cards per page', 4, 4, 68),
    (5, '5x4', '20 cards per page', 5, 4, 100);
