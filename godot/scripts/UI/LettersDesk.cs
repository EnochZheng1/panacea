using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// The morning's post. Each letter is a sheet on its sender's letterhead: the coroner's office
/// with its seal, a diploma mill's flourish, a customer's own hand. Replies are slips under the
/// letter. Answering stamps the sheet and folds it away, and says at once what the answer cost.
/// </summary>
public partial class LettersDesk : VBoxContainer
{
	private readonly IGame _game;
	private string? _note;
	/// <summary>The shop's suspicion when the post was laid out (0..100): a suspected shop's letters earn less.</summary>
	private double _heat;
	public event Action? Changed;

	/// <summary>
	/// Who writes each kind of letter, and what they write, in their own voice. The docket is the
	/// clerk's note of how it came (set in italic above the sheet); {name} is the person the letter
	/// concerns, read off its title.
	/// </summary>
	private sealed record Head(string Sender, string Address, string Sign, string Seal, string Docket, string Salute, string Voice);

	private static readonly Dictionary<string, Head> Heads = new()
	{
		["inquest"] = new("OFFICE OF THE CORONER", "City and County of New-York · Centre Street", "By order of the Coroner, J. DUFFY, Clerk", "seal_coroner",
			"Served at the shop by a boy from Centre street.", "Sir,—",
			"You are hereby notified that a jury will sit this month upon the death of {name}, who is said to have been a customer of yours. The Coroner will hear any evidence you may wish to give as to the composition of your preparations. The Coroner's time is valuable, as is his good opinion."),
		["grateful_letter"] = new("", "Top floor, rear", "Your grateful servant, a customer", "",
			"In a careful hand, on good paper bought for the purpose.", "Dear Doctor,—",
			"Three physicians gave me up, and your tonic gave me back. I am a new woman and my husband says he hardly knows me. If my words would be of use to other sufferers, you have my leave to put them in the papers, with my name, which I should like spelled correctly."),
		["diploma_mill"] = new("THE ECLECTIC MEDICAL COLLEGE OF OHIO", "Diplomas Conferred by Post · Cincinnati", "J. T. MARSH, M.D. (Hon.), Registrar", "seal_college",
			"A printed circular, with the blanks filled in by hand.", "Dear Doctor,—",
			"The Faculty have heard of your work with great interest, and are pleased to offer you the degree of Doctor of Medicine on receipt of fifty dollars. Attendance is not required; examination is waived in cases of evident merit, and all cases are evident. The diploma is suitable for framing."),
		["sanitary_inspector"] = new("DEPARTMENT OF HEALTH · SANITARY BUREAU", "301 Mott Street", "P. QUINLAN, Inspector of Nuisances", "seal_sanitary",
			"Left on the counter by a man in a hard hat, who is still in the back room.", "Sir,—",
			"I am directed to make an inspection of your back premises. Inspections of this kind are thorough, and can run on for weeks, during which the ward will see a Board of Health man in your back room, and talk of it. A small consideration for my trouble has been known to shorten them."),
		["rival_shop_opens"] = new("", "A note pushed under the door", "A friend of the shop", "",
			"Unsigned, in pencil, with a thumbprint.", "",
			"Thought you ought to know. Feeney has opened two doors down and his window is full of bottles the very spit of yours, at a penny less. People will say anything about a man's stock, if they are given a start."),
		["temperance_picket"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"Handed in by a lady, while a dozen more sing hymns on the step.", "Sir,—",
			"The League has caused your remedy to be tasted by a member, who has not been well since. We shall sing outside your door each day until you see the error of your trade, or contribute to the cause of temperance, which we should accept as a first step."),
		["board_of_health"] = new("THE BOARD OF HEALTH OF THE CITY OF NEW-YORK", "Order of the Board", "By order, T. R. KEMBLE, Assistant Sanitary Superintendent", "seal_health",
			"Served with a warrant, by two inspectors and a wagon.", "Sir,—",
			"Complaint having been made of your preparations, the Board directs that the whole of your stock be removed for analysis. The Board will, in the alternative, accept a fine of thirty dollars, and consider the complaint answered."),
		["rival_price_war"] = new("", "A handbill, pushed under the door", "J. FEENEY, Druggist, two doors down", "",
			"Found on the mat, with three more like it in the street.", "",
			"FEENEY'S TONIC, SIXTY-FIVE CENTS THE BOTTLE. Why pay more on Mulberry street? For the next four months, two callers in five whose bottle costs more than mine will try mine instead. Ask anyone. — J. Feeney"),
		["landlord_rent"] = new("", "Upstairs, the front room", "Your obedient servant, M. O'ROURKE, Landlord", "",
			"Brought down by the landlord's boy, who waited for an answer.", "Sir,—",
			"I see the trade is good, and I am glad of it. From this month the rent is a dollar more, and it will stay so. I would sell you the lease outright for sixty dollars, which would bring it a dollar under the old rent for good. The narrow shop across the street is to let at a dollar under the old rent, if you can do with room for eight fewer regulars."),
		["spirits_dearer"] = new("J. & W. HOBART & CO.", "Wholesale Druggists & Importers of Drugs · 81 Pearl Street", "J. & W. HOBART & CO., per T. Hobart", "seal_hobart",
			"A printed circular from Pearl street, with a note in the margin.", "Sir,—",
			"We regret that the distillers have combined, and that grain spirit will be charged at double the old price for six months from your acknowledgment. Should you prefer it, a Jersey distiller outside the ring will supply at the old price for six dollars down; we are not to be named in the matter."),
		["benefit_society"] = new("ST. BRIDGET'S MUTUAL AID SOCIETY", "Meetings Tuesdays · over the bakery, Mott Street", "Your servant, the Secretary (a regular of the shop)", "",
			"Handed across the counter by one of your regulars, in her Sunday hat.", "Dear Doctor,—",
			"The Society would recommend your remedy to its members in return for a subscription of a dollar and a half a month for the year. We think you may count on five more callers a month. The Society asks only that the remedy do its members no harm, which we are sure it will not."),
		["customer_wants_stronger"] = new("", "In pencil, on the back of a handbill", "One of your regulars", "",
			"Left on the counter, folded small.", "",
			"Doc, the tonic dont do what it did. I take two spoons and then four and its like water. You must have something stronger in the back. I can pay. I have to have it."),
		// Events pass 1: the letters of the long middle (playtests/events-pass-1.md).
		["landlord_roof"] = new("", "Upstairs, the front room", "Your obedient servant, M. O'ROURKE, Landlord", "",
			"Brought down by the landlord's boy, with a wet sleeve to show.", "Sir,—", ""),
		["doctor_arrangement"] = new("LUCIUS FENN, M.D.", "Physician & Surgeon · 212 Grand Street · Hours 8 to 10 and 6 to 8", "L. FENN, M.D.", "",
			"On a physician's letter paper, very white.", "Dear Sir,—", ""),
		["parish_bazaar"] = new("ST. URSULA'S CHAPEL", "Mott Street · The Rectory", "Yours in Christ, D. GARVEY, Rector", "",
			"Brought by two children selling raffle tickets, who also sold you one.", "Dear Sir,—", ""),
		["police_collection"] = new("", "Left at the counter by a patrolman, off his beat", "J. MORAN, Wardman, for the Captain", "",
			"Pencilled on the back of a summons form.", "Friend,—", ""),
		["gazette_canvasser"] = new("THE BOWERY GAZETTE", "Advertising Department · 18 Park Row", "H. LINDLEY, Canvasser", "",
			"Brought by the canvasser himself, who stayed for a bottle.", "Sir,—", ""),
		["board_of_pharmacy"] = new("THE BOARD OF PHARMACY OF THE CITY OF NEW-YORK", "Office of the Secretary · Bond Street", "By order of the Board, R. AULT, Secretary", "",
			"A printed form, with your name spelled two ways.", "Sir,—", ""),
		["blizzard_1888"] = new("THE RELIEF COMMITTEE OF THE WARD", "Meeting at the Elizabeth Street station-house", "For the Committee, W. HALLORAN, Secretary", "",
			"Carried over the drifts by a boy in his father's boots.", "Sir,—", ""),
		["grippe_1889"] = new("J. & W. HOBART & CO.", "Wholesale Druggists & Importers of Drugs · 81 Pearl Street", "J. & W. HOBART & CO., per T. Hobart", "seal_hobart",
			"A printed circular from Pearl street, with a sample placard folded in.", "Sir,—", ""),
		["jobber_exclusive"] = new("J. & W. HOBART & CO.", "Wholesale Druggists & Importers of Drugs · 81 Pearl Street", "J. & W. HOBART & CO., per T. Hobart", "seal_hobart",
			"Brought by the Hobarts' traveller, with a fountain pen held out.", "Sir,—", ""),
		["medicine_show_offer"] = new("PROF. GIDEON MARSH'S MEDICINE CONCERT & COMEDY CO.", "On the road · Letters care of the Postmaster, Elmira", "G. MARSH, Lecturer & Proprietor", "",
			"On a handbill, with the Professor's portrait in a frock coat.", "My dear Sir,—", ""),
		["medicine_show_returns"] = new("PROF. GIDEON MARSH'S MEDICINE CONCERT & COMEDY CO.", "Winter quarters · Elmira, N.Y.", "G. MARSH, Lecturer & Proprietor", "",
			"A registered letter, with the money in it, mostly in ones.", "My dear Partner,—", ""),
		["kehoe_thanks"] = new("", "Mulberry Street, top floor, front", "Respectfully, Mrs. M. KEHOE", "",
			"In a careful hand, on a sheet torn from a copybook.", "Dear Sir,—", ""),
		["kehoe_more"] = new("", "Mulberry Street, top floor, front", "Respectfully, Mrs. M. KEHOE", "",
			"The same careful hand, the same copybook.", "Dear Sir,—", ""),
		["kehoe_last"] = new("", "Mulberry Street, top floor, rear", "Mrs. B. DOYLE, for Mrs. Kehoe", "",
			"Another hand. The empty bottle came with it.", "", ""),
		["kehoe_well"] = new("", "Mulberry Street, top floor, front", "Respectfully, Mrs. M. KEHOE", "",
			"The careful hand again, on better paper.", "Dear Sir,—", ""),
		["stowe_tract"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"Handed in by Mrs. Stowe herself, who waited by the door.", "Sir,—", ""),
		["ward_boss_favor"] = new("", "Kelly's back room, any night after six", "ED CASSIDY, Captain of the District, for the Alderman", "",
			"Scrawled on the back of a sample ballot.", "Friend,—", ""),
		["ward_boss_due"] = new("", "Kelly's back room, any night after six", "ED CASSIDY, Captain of the District, for the Alderman", "",
			"Scrawled on a sample ballot, with the Alderman's name ticked.", "Friend,—", ""),
		["feeney_boy"] = new("", "Under the counter, where he leaves his cap", "T. BURKE (your boy)", "",
			"Left in the till, folded around a penny.", "Mister,—", ""),
		["feeney_copy"] = new("", "A handbill, pushed under the door", "J. FEENEY, Druggist, two doors down", "",
			"Found on the mat; the boy's cap was on the corner of Feeney's counter.", "", ""),
		["feeney_inquest"] = new("OFFICE OF THE CORONER", "City and County of New-York · Centre Street", "By order of the Coroner, J. DUFFY, Clerk", "seal_coroner",
			"Served at the shop by a boy from Centre street, who asked for Feeney's.", "Sir,—", ""),
		["pawn_ticket"] = new("D. CRANE, LICENSED PAWNBROKER", "Loans on Watches, Rings & Wearing Apparel · Bayard Street", "D. CRANE", "",
			"A pawn ticket pinned to a printed card.", "Sir,—", ""),
		["widow_account"] = new("", "Elizabeth Street, second floor, rear", "Respectfully, a widow of the ward", "",
			"In a careful hand, with black edges cut from a funeral card.", "Dear Sir,—", ""),
		["soda_fountain_offer"] = new("THE CRYSTAL FOUNT COMPANY", "Soda Water Apparatus · 14 Gold Street", "per W. LOOMIS, Agent", "",
			"With a lithograph of the fountain, and a lady drinking from it.", "Sir,—", ""),
		["undertaker"] = new("SILAS VOORHEES", "Undertaker & Embalmer · Funerals Furnished Complete · Bayard Street", "S. VOORHEES", "",
			"On black-edged paper, by a man in black gloves.", "Dear Sir,—", ""),
		["electric_light"] = new("THE MANHATTAN ELECTRIC ILLUMINATING CO.", "Contract Department · Pearl Street", "per A. STRAUB, Contract Agent", "",
			"With a pamphlet on the safety of the electric current.", "Sir,—", ""),
		["reporter_calls"] = new("THE EVENING CLARION", "City Room · 41 Park Row", "NED PRYOR, of the Clarion", "",
			"Dashed off on copy paper, and delivered by a boy who wanted a reply.", "Dear Doc,—", ""),
		["reporter_proofs"] = new("THE EVENING CLARION", "City Room · 41 Park Row", "NED PRYOR, of the Clarion", "",
			"A galley proof, still damp, with a note pinned to it.", "Dear Doc,—", ""),
		["reporter_kind"] = new("THE EVENING CLARION", "City Room · 41 Park Row", "NED PRYOR, of the Clarion", "",
			"With the cutting enclosed, and a cigar ash on it.", "Dear Doc,—", ""),
		["stowe_chemist"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"Handed in by Mrs. Stowe, with the chemist's certificate held back.", "Sir,—", ""),
		["stowe_petition"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"Brought by Mrs. Stowe, with the petition rolled under her arm.", "Dear Sir,—", ""),
		["proprietary_invite"] = new("THE PROPRIETARY ASSOCIATION OF AMERICA", "Office of the Secretary · New-York", "For the Executive Committee, the Secretary", "",
			"An engraved invitation, with a printed list of the bills it has killed.", "Dear Sir,—", ""),
		["red_clause"] = new("THE PROPRIETARY ASSOCIATION OF AMERICA", "Office of the Secretary · New-York", "For the Executive Committee, the Secretary", "",
			"A circular to members, with the clause printed in red.", "Dear Sir,—", ""),
		["cholera_1892"] = new("THE BOARD OF HEALTH OF THE CITY OF NEW-YORK", "Circular to Druggists", "By order, T. R. KEMBLE, Assistant Sanitary Superintendent", "seal_health",
			"A printed circular, with a printed notice folded inside.", "Sir,—", ""),
		["magazine_household"] = new("THE HEARTHSIDE MONTHLY", "Household Department · Union Square", "MISS E. PRATT, Editress of the Household Page", "",
			"On lilac paper, smelling faintly of violets.", "Dear Sir,—", ""),
		["panic_relief"] = new("THE EAST SIDE RELIEF WORK COMMITTEE", "Rooms at the University Settlement · Delancey Street", "For the Committee, S. GOLDING, Secretary", "",
			"Brought by a man who said he was a cabinetmaker, and was.", "Sir,—", ""),
		["panic_credit"] = new("J. & W. HOBART & CO.", "Wholesale Druggists & Importers of Drugs · 81 Pearl Street", "J. & W. HOBART & CO., per T. Hobart", "seal_hobart",
			"A printed circular from Pearl street, with 'URGENT' in the junior partner's hand.", "Sir,—", ""),
		["lexow_subpoena"] = new("SPECIAL COMMITTEE OF THE SENATE TO INVESTIGATE THE POLICE DEPARTMENT", "Sitting at the County Court House · Chambers Street", "H. VAIL, Clerk to the Committee", "",
			"Served by a process server, who had a list of the street.", "Sir,—", ""),
		["sunday_closing"] = new("", "The Mulberry Bend Café, Lager & Wines, shut Sundays by order", "G. RITTER, Proprietor", "",
			"Brought round on a Sunday, by the side door.", "Neighbour,—", ""),
		["wheelmen"] = new("THE MULBERRY WHEELMEN", "Club-room over the bakery, Grand Street · Runs Sundays at 7", "F. ACKERLY, Captain of the Road", "",
			"Delivered by a young man in knickerbockers, who rode it in.", "Dear Sir,—", ""),
		["trolley_cards"] = new("THE STREET RAILWAY ADVERTISING COMPANY", "Car Cards & Station Space · Nassau Street", "per J. HENNESSY, Solicitor", "",
			"With a sample card, in three colours.", "Sir,—", ""),
		["bicycle_face"] = new("ORRIN GILCHRIST, M.D.", "Specialist in Nervous Diseases · West Twenty-third Street", "O. GILCHRIST, M.D.", "",
			"Typewritten, and signed with a flourish.", "Dear Sir,—", ""),
		["state_chemist"] = new("STATE BOARD OF HEALTH", "Office of the Analyst · The Capitol, Albany", "By direction, W. CORLISS, Analyst", "",
			"An official envelope, franked.", "Sir,—", ""),
		["lodging_house"] = new("THE GLOBE LODGING HOUSE", "Bowery · Beds 10c., 15c. & 25c. · No Women", "M. KRAUSE, Proprietor", "",
			"Brought by the night clerk, who would not sit down.", "Sir,—", ""),
		["tammany_back"] = new("", "Kelly's back room, any night after six", "ED CASSIDY, Captain of the District, for the Alderman", "",
			"Scrawled on the back of a Tammany ticket.", "Friend,—", ""),
		["volunteers_1898"] = new("", "Camp Black, Hempstead Plains · Co. G, Volunteer Infantry", "J. CORCORAN, Q.M. Sergeant, Co. G", "",
			"Brought by a corporal of nineteen, in a uniform too large for him.", "Dear Sir,—", ""),
		["army_contract"] = new("B. F. HOLLISTER", "Contractor to the Commissary Department · Willard's Hotel", "B. F. HOLLISTER", "",
			"On hotel paper, with a war-stamp on the envelope.", "Dear Sir,—", ""),
		["army_inquiry"] = new("COMMISSION TO INVESTIGATE THE CONDUCT OF THE WAR DEPARTMENT", "Sitting at the War Department", "By direction, R. HALE, Recorder", "",
			"An official envelope, with the eagle on it.", "Sir,—", ""),
		["farm_wife"] = new("", "Rural Free Delivery No. 2, Licking County, Ohio", "Mrs. L. CARROW", "",
			"In pencil, on lined paper, with a two-cent stamp.", "Dear Sir,—", ""),
		["testimonial_broker"] = new("THE ELITE TESTIMONIAL BUREAU", "Letters of Cure Supplied · 117 Nassau Street", "per R. DUNNE, Manager", "",
			"A printed circular, with three sample cures in three different hands.", "Dear Sir,—", ""),
		["heroin_traveller"] = new("DYER & PLATT", "Sole Agents for Fine Chemicals of the Rhine · William Street", "per O. HALVORSEN, Traveller", "",
			"Left with a sample tin, and a pamphlet in two languages.", "Sir,—", ""),
		["letter_broker"] = new("W. GEDNEY, DEALER IN LETTERS", "Names and Addresses for Mail-Order Houses · Room 9, 150 Nassau Street", "W. GEDNEY", "",
			"A card, with a price list on the back.", "Sir,—", ""),
		["ice_trust"] = new("THE HUDSON & CROTON ICE COMPANY", "Office of the Consolidated Companies · Broadway", "By order of the Directors", "",
			"A printed card, left with the iceman's bill.", "Sir,—", ""),
		["cut_rate"] = new("THE RETAIL DRUGGISTS' PROTECTIVE ASSOCIATION", "Local Branch · Rooms at the College of Pharmacy", "For the Branch, P. LUNDQUIST, Secretary", "",
			"A circular, with a cut-rate handbill attached showing your bottle.", "Dear Sir,—", ""),
		["grimsby_belt"] = new("DR. GRIMSBY'S GALVANIC BELT CO.", "Restorers of Lost Vigor · 9 Ann Street", "H. GRIMSBY (\"Dr.\")", "",
			"With a Belt enclosed, on thirty days' trial.", "Dear Brother Proprietor,—", ""),
		["tenement_dept"] = new("TENEMENT HOUSE DEPARTMENT OF THE CITY OF NEW YORK", "Bureau of Inspection · 61 Irving Place", "By order, F. MARSH, Chief Inspector", "",
			"A printed order, with the inspector's notes in the margin.", "Sir,—", ""),
		["bureau_sample"] = new("U.S. DEPARTMENT OF AGRICULTURE", "Bureau of Chemistry", "By direction of the Chief, A. KEENE, Clerk", "",
			"An official envelope, franked, with a printed form.", "Sir,—", ""),
		["radium_water"] = new("THE RADIO-VITAL WATER COMPANY", "Laboratories at Newark, N.J.", "per E. FAIRBAIRN, Secretary", "",
			"With a photograph of a glowing bottle, which is a painting.", "Dear Sir,—", ""),
		["motor_car"] = new("BRILL & ASHBY", "Attorneys and Counsellors · 2 Wall Street", "BRILL & ASHBY, for the owner", "",
			"Delivered by a clerk, who looked at the hole in the window and coughed.", "Sir,—", ""),
		["subway_signs"] = new("THE RAPID TRANSIT ADVERTISING CONCESSION", "Station & Car Space · Broadway", "per L. MCALLISTER, Agent", "",
			"With a drawing of the platform, and a sign on every pillar.", "Sir,—", ""),
		["muckraker"] = new("", "Editorial Rooms, a national weekly · Fifth Avenue", "M. ORCUTT, Special Correspondent", "",
			"Typewritten, with a list of questions numbered one to nine.", "Dear Sir,—", ""),
		["reporter_returns"] = new("", "Editorial Rooms, a national weekly · Fifth Avenue", "NED PRYOR, late of the Clarion", "",
			"Typewritten now, but signed in the same hand.", "Dear Doc,—", ""),
		["ama_council"] = new("AMERICAN MEDICAL ASSOCIATION", "Council on Pharmacy and Chemistry · Dearborn Avenue", "By direction of the Council, the Secretary", "",
			"A printed form, with a long list of questions.", "Sir,—", ""),
		["pa_war_chest"] = new("THE PROPRIETARY ASSOCIATION OF AMERICA", "Office of the Secretary · New-York", "For the Executive Committee, the Secretary", "",
			"Marked PERSONAL AND CONFIDENTIAL, twice.", "Dear Sir,—", ""),
		["red_clause_called"] = new("THE PROPRIETARY ASSOCIATION OF AMERICA", "Office of the Secretary · New-York", "For the Executive Committee, the Secretary", "",
			"Marked PERSONAL AND CONFIDENTIAL, three times.", "Dear Sir,—", ""),
		["congress_member"] = new("HOUSE OF REPRESENTATIVES", "Office of the Member for your District", "J. WIMBUSH, Secretary to the Member", "",
			"On House paper, franked.", "Dear Sir,—", ""),
		["stowe_washington"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"In an old lady's hand, slower now, but every word legible.", "Dear Sir,—", ""),
		["label_lawyer"] = new("GRUE & PARTRIDGE", "Counsellors-at-Law · Specialists to the Proprietary Trade · 32 Liberty Street", "GRUE & PARTRIDGE", "",
			"On heavy paper, with the specimen label pasted on, upside down.", "Dear Sir,—", ""),
		["food_not_drug"] = new("A. BLOOMFIELD", "Consulting Chemist to the Trade · Maiden Lane", "A. BLOOMFIELD, Ph.D. (Heidelberg, he says)", "",
			"A card, with a sample label: BLOOMFIELD'S TABLE BEVERAGE (NOURISHING).", "Dear Sir,—", ""),
		["still_smell"] = new("", "Next door, the millinery, first floor", "H. ALBRECHT, Milliner", "",
			"Pinned to a bonnet, which smelled of the still.", "Sir,—", ""),
		["apprentice"] = new("", "Second year, the College of Pharmacy of the City of New-York", "W. ESCHER, Junior Class", "",
			"In a precise hand, with the grains written as a chemist writes them.", "Dear Sir,—", ""),
		["sign_painter"] = new("A. ROSSI", "Sign & Ornamental Painter · Gold Leaf a Specialty · Elizabeth Street", "A. ROSSI", "",
			"With a sketch of the sign in gold paint, which came off on the counter.", "Dear Sir,—", ""),
		["burglary"] = new("", "The station-house, Elizabeth Street", "P. LYNCH, Detective Sergeant", "",
			"Left by a detective in a bowler, who looked at the window and the till.", "Sir,—", ""),
		["sweatshop"] = new("H. KEMP & CO.", "Contractors · Shirtwaists & Wrappers · Walker Street, fourth floor", "H. KEMP", "",
			"On a contractor's billhead, with the piece rates on the back.", "Sir,—", ""),
		["gentleman_morphine"] = new("", "Washington Square, North · do not reply here", "A Gentleman", "",
			"On thick cream paper, the crest cut off with scissors.", "Sir,—", ""),
		["revenue_stamp"] = new("UNITED STATES INTERNAL REVENUE", "Office of the Collector · Second District, New-York", "By direction of the Collector, M. FARRELL, Deputy", "",
			"An official envelope, with a printed form for the tax.", "Sir,—", ""),
		["pushcart"] = new("", "Pushcart No. 412, at the corner of Bayard Street", "S. MARINO, with the cart", "",
			"Brought to the counter with a lemon, which was a gift.", "Dear Sir,—", ""),
		["photographer"] = new("J. HALE", "Photographic Artist · Cabinet Portraits · Bowery", "J. HALE", "",
			"With a sample portrait of a stranger in excellent whiskers.", "Dear Sir,—", ""),
		["obituary_desk"] = new("THE BOWERY GAZETTE", "City Desk · 18 Park Row", "for the City Editor, R. TOLL", "",
			"Brought by a copy boy, who waited.", "Sir,—", ""),
		["phrenologist"] = new("PROF. LYSANDER POOLE", "Practical Phrenologist · Heads Read, 25c. · Upstairs", "L. POOLE, Professor", "",
			"With a chart of the head, the organ of Vitality marked in red.", "Dear Neighbour,—", ""),
		["school_principal"] = new("THE GRAMMAR SCHOOL, MOTT STREET", "Office of the Principal", "E. WARDWELL, Principal", "",
			"On ruled school paper, in the hand that marks the copybooks.", "Sir,—", ""),
		// (end of events pass 1)
		// Events pass 2: the everyday letters (playtests/events-pass-2.md).
		["clerk_applies"] = new("", "Stuyvesant Street, care of Mrs. Healy", "Respectfully, Miss L. PRUITT", "",
			"Typewritten: the first typewritten letter the shop has had from a private person.", "Dear Sir,—", ""),
		["clerk_books"] = new("", "The back room, at the high desk", "L. PRUITT", "",
			"Left on the ledger, open at the page.", "Sir,—", ""),
		["clerk_leaves"] = new("", "Stuyvesant Street, care of Mrs. Healy", "Respectfully, L. PRUITT", "",
			"Typewritten, with the signature in ink and a pressed violet in the fold.", "Dear Sir,—", ""),
		["lodge_invite"] = new("THE INDEPENDENT ORDER OF THE GOLDEN LAMP", "Mulberry Lodge No. 112 · Meets Wednesdays in the hall over the bakery, Grand Street", "G. FAHEY, Noble Recording Scribe", "",
			"With a printed card of the lodge's degrees, which are seven, and its benefits, which are three.", "Sir and Brother (we trust),—", ""),
		["lodge_brother"] = new("THE INDEPENDENT ORDER OF THE GOLDEN LAMP", "Mulberry Lodge No. 112 · Meets Wednesdays in the hall over the bakery, Grand Street", "G. FAHEY, Noble Recording Scribe", "",
			"Brought by Brother Tierney, who gave the grip, and then his hat a turn in his hands.", "Brother,—", ""),
		["lodge_supper"] = new("THE INDEPENDENT ORDER OF THE GOLDEN LAMP", "Mulberry Lodge No. 112 · Meets Wednesdays in the hall over the bakery, Grand Street", "G. FAHEY, Noble Recording Scribe", "",
			"A printed card, with your name written in under the toasts.", "Brother,—", ""),
		["apprentice_exam"] = new("", "The College of Pharmacy of the City of New-York · Graduating Class", "W. ESCHER, Ph.G.", "",
			"In the same precise hand, with the new letters after the name underlined twice.", "Dear Sir,—", ""),
		["carrier_address"] = new("THE CARRIER OF THE BOWERY GAZETTE", "Respectfully presents his Annual Address to his Patrons", "Your Carrier, T. DOWD", "",
			"A printed sheet of verses, with an eagle at the top and the carrier's name inked in.", "", ""),
		["holiday_goods"] = new("J. & W. HOBART & CO.", "Wholesale Druggists & Importers of Drugs · 81 Pearl Street", "J. & W. HOBART & CO., per T. Hobart", "seal_hobart",
			"A printed circular from Pearl street, with a card of the Riviera scent pinned to it.", "Sir,—", ""),
		["ward_turkeys"] = new("THE HOGAN ASSOCIATION", "Club Rooms · Hester Street · Annual Christmas Dinner to the Poor of the District", "M. CARROLL, Secretary, for the Leader", "",
			"A printed appeal, with the Leader's portrait, larger than the turkeys.", "Dear Friend,—", ""),
		["lent_pledge"] = new("", "Baxter Street, the rear house", "Respectfully, D. CONLON", "",
			"In pencil, with a second hand underneath in ink.", "Sir,—", ""),
		["easter_complexion"] = new("", "Elizabeth Street, second floor, front", "Miss K. DEVLIN", "",
			"On scented paper, with a fashion plate pinned to it.", "Dear Sir,—", ""),
		["moving_day"] = new("", "Mott Street, third floor, front (until the first of May)", "Respectfully, Mrs. J. MALLON", "",
			"Brought by her boy, with the Harlem address written twice.", "Dear Sir,—", ""),
		["moving_debts"] = new("THE MERCANTILE COLLECTION AGENCY", "Bad Debts Recovered · Tenants Traced · Nassau Street", "per E. KLINE, Collector", "",
			"On a printed form, with the six names already filled in.", "Sir,—", ""),
		["summer_fans"] = new("THE PERFECTION FAN WORKS", "Palm-Leaf & Paper Fans, Printed to Order · Beekman Street", "per H. GOLD", "",
			"With a sample fan, printed with a stranger's name.", "Dear Sir,—", ""),
		["summer_syrups"] = new("THE BOARD OF HEALTH OF THE CITY OF NEW-YORK", "Circular to Druggists", "By order, T. R. KEMBLE, Assistant Sanitary Superintendent", "seal_health",
			"A printed circular, the same every June, with this year's figures.", "Sir,—", ""),
		["fourth_of_july"] = new("THE MULBERRY STREET FOURTH OF JULY COMMITTEE", "A Band, Fireworks & an Oration · Headquarters at the corner grocery", "P. KEARNS, Treasurer", "",
			"Brought by two boys with a cigar box for the money, already rattling.", "Dear Sir,—", ""),
		["excursion"] = new("ST. URSULA'S CHAPEL", "Mott Street · The Sunday School", "Yours in Christ, D. GARVEY, Rector", "",
			"On the Sunday school's paper, with a programme of the day: hymns, races and a sermon on the barge.", "Dear Sir,—", ""),
		["snow_sidewalk"] = new("DEPARTMENT OF STREET CLEANING", "Office of the Commissioner", "By order of the Commissioner, per J. FOLEY, Deputy", "",
			"A printed notice, with your number written in, and the lady's name.", "Sir,—", ""),
		["coal_dealer"] = new("J. HARKNESS & SON", "Coal & Wood · Anthracite Delivered · Foot of East Eleventh Street", "J. HARKNESS & SON, per the son", "",
			"On a coal dealer's billhead, with a smudge where the son held it.", "Sir,—", ""),
		["calendar_printer"] = new("THE KNICKERBOCKER LITHOGRAPHIC CO.", "Calendars, Show Cards & Labels · Rose Street", "per F. VAN AKEN", "",
			"With a specimen calendar, the kitten in full colour.", "Dear Sir,—", ""),
		["cloudburst"] = new("M. GILHOOLY", "Plumber, Gas & Steam Fitter · Jobbing Promptly Attended To · Baxter Street", "M. GILHOOLY", "",
			"Brought by the plumber, in boots, who had already looked.", "Sir,—", ""),
		["school_prize"] = new("THE GRAMMAR SCHOOL, MOTT STREET", "Office of the Principal", "E. WARDWELL, Principal", "",
			"On ruled school paper, in the hand that marks the copybooks.", "Sir,—", ""),
		["choir_lozenges"] = new("ST. URSULA'S CHAPEL", "Mott Street · The Choir · Rehearsals Thursdays", "A. PEMBERTON, Organist & Choirmaster", "",
			"On the back of a page of the alto part.", "Dear Sir,—", ""),
		["target_excursion"] = new("THE FLYNN GUARD", "Annual Target Excursion & Picnic · Headquarters, Kelly's", "T. COSGROVE, Captain", "",
			"Brought by two of the Guard, in sashes, who admired the window.", "Friend,—", ""),
		["labor_day"] = new("THE UNITED TRADES OF THE SIXTH WARD", "Delegates' Rooms over the Harmony Hall · Meetings Sundays", "For the Delegates, J. RAFFERTY, Recording Secretary", "",
			"With the card for the window, printed in red.", "Dear Sir,—", ""),
		["slate_credit"] = new("", "Hester Street, rear house, ground floor", "Mrs. A. FINNERTY", "",
			"On a sheet from a child's copybook, with the sums worked in the margin.", "Dear Sir,—", ""),
		["wrong_bottle"] = new("", "Bayard Street, over the harness shop", "W. TRASK", "",
			"Wrapped round a bottle of your liniment, two spoonfuls gone.", "Sir,—", ""),
		["horse_order"] = new("M. BRADY", "Livery & Boarding Stable · Coaches for Weddings & Funerals · Elm Street", "M. BRADY", "",
			"Smelling of the stable, on the back of a feed merchant's bill.", "Sir,—", ""),
		["thanks_note"] = new("", "Mott Street, top floor, front", "Respectfully, Mrs. R. QUAID", "",
			"With a pen-wiper of red flannel, in the shape of a mortar and pestle, more or less.", "Dear Sir,—", ""),
		["window_ball"] = new("", "Mulberry Street, the basement", "Respectfully, J. CULLEN (father of Michael)", "",
			"Brought by Michael, who stood in the doorway with his cap in his hands.", "Sir,—", ""),
		["bad_note"] = new("THE MULBERRY & BAYARD BANK", "Office of the Receiving Teller", "R. SCHOONMAKER, Receiving Teller", "",
			"With the note enclosed, stamped COUNTERFEIT in violet ink.", "Sir,—", ""),
		["bottle_boys"] = new("I. GOLDMAN", "Dealer in Rags, Bottles & Old Metals · Baxter Street", "I. GOLDMAN", "",
			"On a dealer's card, with the count of bottles written on the back.", "Dear Sir,—", ""),
		["ship_chest"] = new("THE BARQUE MARY ELLEN, OF BATH", "Lying at Pier 14, East River · Sails for Valparaiso on the tide of the ninth", "J. COFFIN, Master", "",
			"Brought by the mate, who waited, and bought a bottle for himself.", "Sir,—", ""),
		["theatre_order"] = new("THE PEOPLE'S LYCEUM", "Melodrama Nightly · Matinées Wednesday & Saturday · The Bowery", "for the Management, C. DUVAL, Stage Manager", "",
			"With two passes for the gallery, and a handbill of the villain.", "Dear Sir,—", ""),
		["history_book"] = new("THE HISTORICAL & BIOGRAPHICAL PUBLISHING CO.", "Subscription Department · Astor Place", "per H. W. PELL, Canvasser", "",
			"With a specimen page: a brewer, engraved, and a page and a half on his virtues.", "Dear Sir,—", ""),
		["fire_insurance"] = new("THE PRUDENT FIRE INSURANCE COMPANY", "Agency Department · Pine Street", "per D. LORING, Agent", "",
			"With the surveyor's sketch of the back room, the barrel marked X.", "Sir,—", ""),
		["charity_appeal"] = new("THE SOCIETY FOR THE RELIEF OF DESTITUTE SEAMEN'S ORPHANS", "Rooms, Nassau Street (third floor, back)", "Rev. E. T. LOVEJOY, Agent", "",
			"A printed appeal. The sergeant at the station-house says the Reverend was in the Tombs at Easter, under another name.", "Dear Friend,—", ""),
		["doctor_counter"] = new("J. A. RUTLEDGE, M.D.", "Physician to the Out-Door Poor · Hours at the Dispensary 12 to 2", "J. A. RUTLEDGE, M.D.", "",
			"On a dispensary prescription blank, both sides.", "Sir,—", ""),
		["tonic_taste"] = new("", "Elizabeth Street, first floor, rear", "Your customer, and your predecessor's, P. HAGGERTY", "",
			"On the back of the tonic's own label, soaked off the bottle.", "Sir,—", ""),
		["child_errand"] = new("", "Baxter Street, the rear house", "J. MCKEON", "",
			"A pencilled note, brought by a girl of eight with a tin jug.", "", ""),
		["carboy"] = new("WM. DEGNAN", "Truckman · Carting & Expressing · Stand at Pearl and Fulton Streets", "W. DEGNAN", "",
			"In a large hand, on a carter's way-bill.", "Sir,—", ""),
		["label_printer"] = new("O. KESSLER, PRINTER", "Book & Job Printing · Labels, Cards & Circulars · Rose Street", "O. KESSLER", "",
			"With a specimen label, on which the compositor has set CURSES for CURES.", "Sir,—", ""),
		["jobber_rival"] = new("LEHMANN & STROUD", "Wholesale Druggists · Drugs, Chemicals & Sundries · Maiden Lane", "per R. STROUD", "",
			"Left by their traveller, who had a list of your last six orders.", "Dear Sir,—", ""),
		["neighbour_fire"] = new("", "The front house next door, what is left of it", "Mrs. E. RAFFERTY, for the tenants", "",
			"On the back of a fire insurance notice, singed at one corner.", "Dear Sir,—", ""),
		["agent_paint"] = new("J. P. WYCKOFF", "Real Estate · Agent for the Estate · Rents Collected & Repairs Attended To · Second Avenue", "per J. P. WYCKOFF, Agent for the Estate", "",
			"A printed form, with clause nine copied out in red ink.", "Sir,—", ""),
		["agent_noxious"] = new("J. P. WYCKOFF", "Real Estate · Agent for the Estate · Rents Collected & Repairs Attended To · Second Avenue", "per J. P. WYCKOFF, Agent for the Estate", "",
			"With clause four copied out, and the tenant's complaint, unsigned, attached.", "Sir,—", ""),
		["boy_measles"] = new("", "Mott Street, the rear house", "Mrs. N. BURKE", "",
			"Brought by Joseph, who stood very straight.", "Dear Sir,—", ""),
		["wedding_punch"] = new("", "Hester Street, over the bakery", "Respectfully, P. NOONAN (father of the bride)", "",
			"On the back of a wedding invitation, which is also enclosed.", "Dear Sir,—", ""),
		["perfume_copy"] = new("MADAME ROSALIE", "Modes & Millinery · Grand Street", "MME. ROSALIE (Mrs. R. Duffy)", "",
			"On pink paper, with a magazine advertisement cut out and pinned to it.", "Dear Sir,—", ""),
		["sealer"] = new("OFFICE OF THE INSPECTOR OF WEIGHTS & MEASURES", "City of New-York · Room 9, the City Hall", "per J. KEARNEY, Deputy Sealer", "",
			"A printed notice, with the deputy's figures in the margin.", "Sir,—", ""),
		["night_bell"] = new("", "Mulberry Street, third floor, rear", "Respectfully, Mrs. T. SHEEHY", "",
			"In a tired hand, in pencil.", "Dear Sir,—", ""),
		["jury_duty"] = new("OFFICE OF THE COMMISSIONER OF JURORS", "City and County of New-York · Chambers Street", "By order, the Commissioner, per W. TOOHEY, Clerk", "",
			"Served by a deputy, who also had one for the grocer.", "Sir,—", ""),
		["pedestal_fund"] = new("THE SIXTH WARD COMMITTEE FOR THE PEDESTAL FUND", "Collections at the Ward's Schools, Saloons & Places of Business", "J. MURTAGH, Treasurer", "",
			"With a tin box, painted with the statue, and a slot.", "Dear Sir,—", ""),
		["centennial_1889"] = new("THE CENTENNIAL COMMITTEE OF THE WARD", "Decorations for the Washington Centennial · Rooms at Kelly's", "for the Committee, R. FINN", "",
			"With a sample of the bunting, a little faded.", "Dear Sir,—", ""),
		["trade_cards"] = new("THE KNICKERBOCKER LITHOGRAPHIC CO.", "Calendars, Show Cards & Labels · Rose Street", "per F. VAN AKEN", "",
			"With twelve specimen cards, which the boy took.", "Dear Sir,—", ""),
		["census_1890"] = new("ELEVENTH CENSUS OF THE UNITED STATES", "Office of the Supervisor · Schedule of Manufactures", "By direction of the Supervisor, T. MAHER, Special Agent", "",
			"A long printed schedule, with the instructions on four pages.", "Sir,—", ""),
		["columbus_1892"] = new("THE COLUMBIAN CELEBRATION COMMITTEE OF THE MULBERRY DISTRICT", "Rooms on Mulberry Street, at the Mutual Aid Society", "For the Committee, A. VENTURA, Secretary", "",
			"With a card of the procession's order: the societies, the bands, and the druggists last.", "Dear Sir,—", ""),
		["fair_medal_1893"] = new("THE INTERNATIONAL BUREAU OF AWARDS", "Diplomas & Medals for Exhibitors at the World's Columbian Exposition · Chicago", "per Col. J. B. TRIMBLE, Commissioner", "",
			"With a drawing of the medal, both sides, and a ribbon.", "Dear Sir,—", ""),
		["phonograph"] = new("THE AUTOMATIC PHONOGRAPH PARLOR", "Hear the Latest Songs, 5c. · Next Door but One", "per S. BLOOM, Manager", "",
			"Typewritten, with a cylinder enclosed that nobody in the shop can play.", "Dear Sir,—", ""),
		["settlement_nurse"] = new("THE DISTRICT NURSES' HOUSE", "Visiting Nurses for the Sick Poor · Eldridge Street", "H. CARVER, Visiting Nurse", "",
			"Brought by the nurse herself, in her grey uniform, who waited for the answer.", "Dear Sir,—", ""),
		["antitoxin_1895"] = new("THE BOARD OF HEALTH OF THE CITY OF NEW-YORK", "Circular to Druggists", "By order, T. R. KEMBLE, Assistant Sanitary Superintendent", "seal_health",
			"A printed circular, with the Department's laboratory pictured at the top.", "Sir,—", ""),
		["white_wings"] = new("DEPARTMENT OF STREET CLEANING", "Office of the Commissioner", "By order of the Commissioner, per J. FOLEY, Deputy", "",
			"A printed card of the new rules, with three barrels pictured.", "Sir,—", ""),
		["bicycle_boy"] = new("", "Under the counter, where he leaves his cap", "T. BURKE (your boy)", "",
			"Left in the till, with a picture of a bicycle cut from a newspaper.", "Mister,—", ""),
		["x_rays_1896"] = new("PROF. AUGUSTUS VAIL, ELECTRICIAN", "Exhibitions of the X Rays, or Roentgen Rays · 10 Cents a Hand", "A. VAIL, Professor", "",
			"With a photograph of a hand, the ring very clear.", "Dear Sir,—", ""),
		["gold_clause_1896"] = new("J. & W. HOBART & CO.", "Wholesale Druggists & Importers of Drugs · 81 Pearl Street", "J. & W. HOBART & CO., per T. Hobart", "seal_hobart",
			"A printed circular from Pearl street, with the agreement attached.", "Sir,—", ""),
		["raines_law"] = new("THE MULBERRY BEND HOTEL", "Formerly the Mulberry Bend Café · Ten Rooms · Meals Served Sundays", "G. RITTER, Proprietor", "",
			"Brought round on a Sunday, by the front door this time.", "Neighbour,—", ""),
		["school_inspector"] = new("DEPARTMENT OF HEALTH", "Medical Inspection of Schools", "per E. ROSSITER, M.D., Medical Inspector", "",
			"With the formula written out, and the price underlined.", "Sir,—", ""),
		["consolidation_1898"] = new("THE CITY OF NEW YORK", "Office of the Mayor · Bureau of Licenses", "per F. DOLAN, Clerk", "",
			"A printed form, with the new seal, and the old one crossed out.", "Sir,—", ""),
		["war_stamps_1898"] = new("UNITED STATES INTERNAL REVENUE", "Office of the Collector · Second District, New-York", "By direction of the Collector, M. FARRELL, Deputy", "",
			"An official envelope, with a sheet of specimen stamps.", "Sir,—", ""),
		["telephone"] = new("THE TELEPHONE EXCHANGE", "Contract Department · Spring Street", "per W. HAINES, Contract Agent", "",
			"With a card of the exchange's subscribers, a third of them physicians.", "Sir,—", ""),
		["liquid_air"] = new("THE NEW YORK LIQUID AIR POWER & REFRIGERATING CO.", "Offices, Broad Street · Works to be Built", "per E. G. FAIRCLOTH, Treasurer", "",
			"With a prospectus, and a photograph of a kettle boiling on a block of ice.", "Dear Sir,—", ""),
		["subway_digging"] = new("THE CONTRACTORS FOR SECTION TWO", "Rapid Transit Railroad · Field Office on Elm Street", "per E. BRADY, Section Engineer", "",
			"On a contractor's letterhead, with a sketch of the plank.", "Sir,—", ""),
		["smallpox_1901"] = new("DEPARTMENT OF HEALTH OF THE CITY OF NEW YORK", "Circular to Druggists", "By order, T. R. KEMBLE, Sanitary Superintendent", "seal_health",
			"A printed circular, with a printed notice folded inside.", "Sir,—", ""),
		["trading_stamps"] = new("THE GOLDEN EAGLE TRADING STAMP CO.", "Premium Parlors · Sixth Avenue", "per J. ROSEN, District Agent", "",
			"With a stamp book, three stamps already pasted in, as a start.", "Dear Sir,—", ""),
		["motorist"] = new("", "In the street outside, in a motor-car", "R. VAN DEUSEN", "",
			"On the back of a visiting card, sent in by a boy who was minding the machine for a nickel.", "Sir,—", ""),
		["postcards"] = new("THE METROPOLITAN POST CARD CO.", "Souvenir Views & Advertising Cards · Lower Broadway", "per A. KRAUS", "",
			"With a card of the Flatiron Building, and one of a butcher's shop, equally proud.", "Dear Sir,—", ""),
		["electric_sign"] = new("THE FLASHING SIGN CO.", "Electric Signs & Illuminations · West Broadway", "per H. MOSS, Contract Agent", "",
			"With a drawing of the sign, every lamp inked in yellow.", "Sir,—", ""),
		["nickelodeon"] = new("THE BIJOU THEATRE OF ANIMATED PICTURES", "Five Cents · Formerly the Cigar Store, Next Door", "per M. LEVITT, Manager", "",
			"With two tickets, and a lantern slide of a stranger's bakery.", "Dear Sir,—", ""),
		// (end of events pass 2)
		// the letters fix of round 8: follow-ups, and a new carrier
		["burglary_book"] = new("", "The station-house, Elizabeth Street", "P. LYNCH, Detective Sergeant", "",
			"Left by the same detective, who did not look at the window this time.", "Sir,—", ""),
		["thanks_sister"] = new("", "Elizabeth Street, second floor, rear", "Respectfully, Mrs. B. GARRITY", "",
			"With your sample bottle, empty, and rinsed.", "Dear Sir,—", ""),
		["bad_note_trial"] = new("UNITED STATES SECRET SERVICE", "New-York District · The Post Office Building", "per J. A. BRADEN, Operative", "",
			"Served by the operative himself, who had your description of the man in his notebook.", "Sir,—", ""),
		["carrier_address_new"] = new("THE CARRIER OF THE BOWERY GAZETTE", "Respectfully presents his Annual Address to his Patrons", "Your Carrier, J. DOWD (brother of the last)", "",
			"A printed sheet of verses, with a bicycle at the top and the new carrier's name inked in.", "", ""),
		// (end of the letters fix)
	};

	/// <summary>What each answer to the coroner risks, said plainly under the slip.</summary>
	private static readonly Dictionary<string, string> Risks = new()
	{
		["inquest.testify"] = "Your word helps a mild bottle, and hurts a strong one: the jury's chemist tests whatever you bring. If they name your bottle, suspicion rises 24 and your standing falls.",
		["inquest.stay_silent"] = "No chemistry is heard, which suits a strong bottle. If the jury names your bottle anyway, suspicion rises 24 and your standing falls.",
		["inquest.bribe_coroner"] = "The verdict you pay for. His fee rises with every envelope and with the talk in the ward, and an envelope can come to light: a bought verdict found out is worse than a jury's.",
		["grateful_letter.print_testimonial"] = "Standing in the ward +3. A testimonial in print is half of what the Gazette asks before it sells you space; the other half is $100 put by.",
		["grateful_letter.file_away"] = "Nothing gained, nothing risked.",
		["diploma_mill.buy_diploma"] = "$50, and you may style yourself Doctor: a new claim for the type case. You become better known, for better or worse.",
		["diploma_mill.decline"] = "Costs nothing.",
		["sanitary_inspector.pay_fee"] = "$3, and he is gone by dinner. You become a little better known at the Department.",
		["sanitary_inspector.let_him_look"] = "Free, but the ward sees him at it: standing −2.",
		["rival_shop_opens.spread_rumour"] = "Standing +1 for now, and you become better known, which the coroner may notice too.",
		["rival_shop_opens.ignore"] = "Standing −3: the ward takes his side.",
		["temperance_picket.donate"] = "$5, and the ladies move on: suspicion −5.",
		["temperance_picket.ignore_them"] = "Free, but the hymns cost you standing −2.",
		["board_of_health.pay_fine"] = "$30, and the complaint is answered: suspicion −10.",
		["board_of_health.surrender_stock"] = "Every bottle on the shelf is taken away; suspicion −20.",
		["customer_wants_stronger.promise_stronger"] = "Standing +1 with the regular. Keeping the promise is up to the formula book.",
		["customer_wants_stronger.recommend_rest"] = "Good advice, and nothing changes.",
		["rival_price_war.handbills"] = "$10, and Feeney takes nothing from you.",
		["rival_price_war.cry_him_down"] = "Free, and Feeney takes nothing, but suspicion +4 and standing −2.",
		["rival_price_war.let_him_try"] = "For four months he takes two callers in five from any bottle dearer than 65c.",
		["landlord_rent.buy_the_lease"] = "$60 now, and the rent falls a dollar a month, for good.",
		["landlord_rent.smaller_premises"] = "Rent a dollar under the old, for good, but room for eight fewer regulars.",
		["landlord_rent.pay_the_rise"] = "Rent up a dollar a month, for good.",
		["spirits_dearer.outside_distiller"] = "$6 now, and spirit stays at the old price.",
		["spirits_dearer.pay_the_new_price"] = "Grain spirit costs double for six months.",
		["benefit_society.subscribe"] = "$1.50 a month for a year, and about five more callers a month.",
		["benefit_society.decline_politely"] = "Nothing changes.",
		// Events pass 1: the letters of the long middle (playtests/events-pass-1.md).
		["landlord_roof.pay_slater"] = "$5, and the roof is sound.",
		["landlord_roof.rent_it"] = "Rent up 50c a month for a year: $6 in all, a little at a time.",
		["landlord_roof.pan_under"] = "Free, but the customers see the pan: standing −1, suspicion +1.",
		["doctor_arrangement.accept"] = "$1 a month for a year, and about three more callers a month, on his prescriptions.",
		["doctor_arrangement.decline"] = "Free, but he speaks against your tonic at the bedside: suspicion +2.",
		["parish_bazaar.donate_dozen"] = "$3; standing +1, and about three more callers a month for two months, sent from the pulpit.",
		["parish_bazaar.send_dollar"] = "$1, and standing +1.",
		["parish_bazaar.regrets"] = "Free, but the parish notices: standing −1.",
		["police_collection.give_five"] = "$5; suspicion −4. Somebody keeps a book of who gave.",
		["police_collection.refuse"] = "Free, but the precinct takes an interest: suspicion +2.",
		["gazette_canvasser.buy_notice"] = "$4, and about four more callers a month for two months.",
		["gazette_canvasser.wait"] = "Costs nothing.",
		["board_of_pharmacy.register"] = "$5; standing +1: a framed certificate behind the counter.",
		["board_of_pharmacy.buy_certificate"] = "$3, for a certificate nobody looks at closely. You become better known; suspicion +2.",
		["board_of_pharmacy.file_it"] = "Free, but the Board marks you down: suspicion +5.",
		["blizzard_1888.give_credit"] = "About $4 that never comes back; standing +1, and the talk in the ward softens.",
		["blizzard_1888.raise_prices"] = "About $5 more this month; standing −3, and you become better known.",
		["blizzard_1888.cash_only"] = "Standing −1: the Committee remembers.",
		["grippe_1889.placards"] = "$2, and about six more callers a month for two months; suspicion +4.",
		["grippe_1889.bed_rest"] = "Standing +1.",
		["jobber_exclusive.sign"] = "$10 down, and every ingredient a tenth cheaper for a year.",
		["jobber_exclusive.stay_free"] = "Costs nothing.",
		["medicine_show_offer.sign_on"] = "$15 now. What comes back depends on the summer, and on the Professor.",
		["medicine_show_offer.decline"] = "Costs nothing.",
		["medicine_show_returns.take_money"] = "$21: the summer paid $6 over its cost.",
		["medicine_show_returns.ask_for_books"] = "$29 when he finds what he overlooked; but he tells the canal towns about you: standing −1, and you become better known.",
		["kehoe_thanks.send_syrup"] = "A bottle on the house (30c); standing +1. She will be back for more.",
		["kehoe_thanks.send_to_dispensary"] = "Standing −1 for now: she hoped you had something.",
		["kehoe_more.stronger"] = "She buys more: about $1 this month. Standing +1, suspicion +2.",
		["kehoe_more.warn_her"] = "Suspicion +3: the doctor asks where the syrup came from. The talk in the ward softens.",
		["kehoe_more.same_again"] = "About 50c this month.",
		["kehoe_last.pay_doctor"] = "$10; suspicion −2, and the talk in the ward softens a little.",
		["kehoe_last.see_the_doctor"] = "$25; suspicion −6, but you become better known, and the top floor knows what you did.",
		["kehoe_last.say_nothing"] = "Suspicion +5, and the ward hears of it.",
		["kehoe_well.ask_to_print"] = "Standing +1, and a testimonial in print; you become a little better known.",
		["kehoe_well.thank_her"] = "About two more callers a month for six months: Mrs. Doyle, and her sister.",
		["stowe_tract.answer_truly"] = "Standing +1, suspicion +2: the League now knows what is in it.",
		["stowe_tract.purely_vegetable"] = "Standing +1. The League keeps its sheets.",
		["stowe_tract.wrap_a_bottle"] = "Suspicion +1.",
		["ward_boss_favor.picture"] = "Suspicion −8, and you become better known. The Alderman's friends are remembered, both ways.",
		["ward_boss_favor.decline"] = "Suspicion +2: the Sanitary Bureau hears you have no friends.",
		["ward_boss_due.supply"] = "$15; suspicion −6. Your name is in the Alderman's book.",
		["ward_boss_due.bottles_only"] = "$6 in bottles; suspicion −1, and you become a little better known.",
		["ward_boss_due.refuse"] = "Suspicion +8: the Sanitary Bureau remembers you after all.",
		["feeney_boy.raise"] = "$2 a month for a year, and the jug stays a secret.",
		["feeney_boy.let_him_go"] = "Free. Feeney gets a sweeper, and perhaps a receipt.",
		["feeney_copy.lawyer"] = "$20, and the bottles come out of his window. Standing +1.",
		["feeney_copy.cry_imitation"] = "For eight months he takes one caller in seven from any bottle dearer than 45c. Suspicion +2; you become better known.",
		["feeney_copy.let_him"] = "For eight months he takes two callers in five from any bottle dearer than 45c.",
		["feeney_inquest.disown"] = "Standing +1, suspicion +3: your receipt is talked of at an inquest. You become better known.",
		["feeney_inquest.buy_feeney_out"] = "$60. Feeney's shop shuts, and about two of his callers a month come to you, for good. Suspicion −2.",
		["feeney_inquest.say_nothing"] = "Suspicion +4, and the ward hears that your receipt killed a man.",
		["pawn_ticket.redeem_it"] = "$1.50; standing +1, and the talk in the ward softens a little.",
		["pawn_ticket.sell_ticket"] = "$1; standing −2, and the ward hears of it.",
		["pawn_ticket.drawer"] = "Nothing changes. The ring stays pledged.",
		["widow_account.close_account"] = "Standing +1, and the talk in the ward softens a little.",
		["widow_account.take_watch"] = "$3; standing −2, and the ward hears of it.",
		["widow_account.ten_cents"] = "About $2 comes in, in dimes.",
		["soda_fountain_offer.install"] = "$60, and about two more callers a month, for good.",
		["soda_fountain_offer.decline"] = "Costs nothing.",
		["undertaker.shake_hands"] = "About two more callers a month for six months, on his word; standing −2, and you become better known.",
		["undertaker.show_the_door"] = "Costs nothing.",
		["electric_light.wire_window"] = "$35, then 50c a month for the current; about two more callers a month, for good.",
		["electric_light.gas_will_do"] = "Costs nothing.",
		["reporter_calls.show_him"] = "Standing +1, and you become better known. What he writes depends on what he sees.",
		["reporter_calls.pay_him"] = "$5. A paid man stays paid only so long.",
		["reporter_calls.show_the_door"] = "Free, but a refused reporter writes anyway.",
		["reporter_proofs.buy_the_head"] = "$25, and the piece runs as a puff: suspicion −2. He will remember the price.",
		["reporter_proofs.pull_the_ads"] = "The editor kills the piece, but Park Row hears why: suspicion +3, and you become better known.",
		["reporter_proofs.let_it_run"] = "Suspicion +8, standing −3, and the ward reads it.",
		["reporter_kind.send_patients"] = "About four more callers a month for three months; standing +1, suspicion +2, and you become better known.",
		["reporter_kind.leave_it"] = "Standing +1.",
		["stowe_chemist.pay_for_hall"] = "$20; suspicion −4, and the certificate stays in a drawer.",
		["stowe_chemist.own_chemist"] = "$12 for a certificate of your own; you become better known, suspicion +1.",
		["stowe_chemist.let_them_print"] = "Suspicion +6, standing −3.",
		["stowe_petition.sign_first"] = "Standing +2, and you become better known; the wholesale houses charge a tenth more for a year.",
		["stowe_petition.decline"] = "Costs nothing.",
		["proprietary_invite.join"] = "$25; suspicion −3. The Association will ask for more when there is a bill.",
		["proprietary_invite.decline"] = "Costs nothing.",
		["red_clause.add_clause"] = "Suspicion −2, and you become a little better known. The papers you advertise in now have an interest in the law.",
		["red_clause.plain_contract"] = "Costs nothing.",
		["cholera_1892.cholera_card"] = "About eight more callers a month for two months; suspicion +6.",
		["cholera_1892.post_notice"] = "Standing +1, suspicion −2.",
		["magazine_household.household_cordial"] = "$20, and about four more callers a month for four months; you become a little better known.",
		["magazine_household.decline"] = "Costs nothing.",
		["panic_relief.honour_tickets"] = "About $20 of medicine at cost; standing +1, and the talk in the ward softens.",
		["panic_relief.decline"] = "Standing −2.",
		["panic_credit.settle"] = "$30 now.",
		["panic_credit.on_credit"] = "Every ingredient costs a tenth more for six months.",
		["lexow_subpoena.testify"] = "Standing +2, suspicion −6, and you become well known. The ward's friends will not forget it.",
		["lexow_subpoena.forget"] = "Suspicion +5, standing −2: the papers print who remembered nothing.",
		["lexow_subpoena.catskills"] = "$40, and the Committee sits without you. You become a little better known.",
		["sunday_closing.sunday_bitters"] = "About $40 over the summer; suspicion +6, standing −1. The Board has plain-clothes men too.",
		["sunday_closing.by_the_bottle"] = "About $10; suspicion +2.",
		["sunday_closing.keep_sabbath"] = "Standing +1.",
		["wheelmen.club_price"] = "About $10 in dimes off, and three more callers a month for six months.",
		["wheelmen.decline"] = "Costs nothing.",
		["trolley_cards.take_cards"] = "$4 a month for a year, and about four more callers a month.",
		["trolley_cards.decline"] = "Costs nothing.",
		["bicycle_face.endorsement"] = "$15, and about four more callers a month for four months; you become better known.",
		["bicycle_face.get_off"] = "Standing +1.",
		["state_chemist.observations"] = "$30; suspicion −5, and you become better known in Albany.",
		["state_chemist.let_it_print"] = "Suspicion +4: juries read the report. Standing +1, for having nothing to say.",
		["lodging_house.by_the_gallon"] = "About $40; suspicion +4, and the Bowery talks.",
		["lodging_house.hire_a_watchman"] = "Costs nothing.",
		["tammany_back.pay_club"] = "$100, and the list is mislaid.",
		["tammany_back.to_the_papers"] = "Standing +1, suspicion +4, and you become better known.",
		["tammany_back.let_them_come"] = "Suspicion +10, standing −1.",
		["volunteers_1898.as_a_gift"] = "$20; standing +1, and the talk in the ward softens.",
		["volunteers_1898.at_cost"] = "About $5 of margin given up; standing +1.",
		["volunteers_1898.cannot_spare"] = "Standing −2.",
		["army_contract.sign_certificate"] = "$80 in royalties; suspicion +4, and you become better known. Your name is on a War Department file.",
		["army_contract.decline"] = "Costs nothing.",
		["army_inquiry.blame_contractor"] = "$20 for the journey; suspicion +2, standing +1.",
		["army_inquiry.write_a_hope"] = "Suspicion +5, and you become better known.",
		["army_inquiry.say_nothing"] = "Suspicion +8, standing −2.",
		["farm_wife.send_drops"] = "$1 by post; suspicion +2, and you become a little better known.",
		["farm_wife.write_honestly"] = "Standing +1.",
		["testimonial_broker.buy_hundred"] = "$6; standing +1 while they are believed, suspicion +2, and you become better known.",
		["testimonial_broker.decline"] = "Costs nothing.",
		["heroin_traveller.fair_trial"] = "Every opiate a third cheaper for a year. The new article goes in your bottle only if you put it there.",
		["heroin_traveller.decline"] = "Costs nothing.",
		["letter_broker.sell_letters"] = "$15; suspicion +2. Your correspondents will hear from strangers, and say so.",
		["letter_broker.burn_them"] = "Costs nothing.",
		["ice_trust.pay_the_price"] = "$25 for the summer's ice.",
		["ice_trust.jersey_ice"] = "$10, for ice nobody has tested: suspicion +2.",
		["ice_trust.shut_fountain"] = "The fountain's two callers a month stay away for four months.",
		["cut_rate.sign_plan"] = "$30, and the cutters lose your bottle.",
		["cut_rate.let_them_cut"] = "For six months one caller in four goes to the cutters, for any bottle dearer than 50c.",
		["grimsby_belt.partner"] = "About three more callers a month for six months; suspicion +2, and you become better known.",
		["grimsby_belt.decline"] = "Costs nothing.",
		["tenement_dept.fireproof"] = "$80, and the order is satisfied. Standing +1.",
		["tenement_dept.ask_inspector"] = "$20; suspicion +2, and you become better known. The new Department is not the old Bureau.",
		["tenement_dept.show_cause"] = "Suspicion +6.",
		["bureau_sample.special_batch"] = "Suspicion −3, and you become a little better known. The Bureau buys its own bottles too.",
		["bureau_sample.from_the_shelf"] = "Standing +1, suspicion +3.",
		["bureau_sample.mislay_it"] = "Suspicion +2.",
		["radium_water.buy_licence"] = "$40, and about four more callers a month for six months; you become better known.",
		["radium_water.decline"] = "Costs nothing.",
		["motor_car.settle"] = "$20.",
		["motor_car.tell_the_gazette"] = "Standing +1, you become better known, and about eight callers come to see the hole this month.",
		["subway_signs.take_signs"] = "$12 a month for a year, and about six more callers a month.",
		["subway_signs.decline"] = "Costs nothing.",
		["muckraker.send_formula"] = "Standing +1, suspicion +4: what is in it will be in print. You become better known.",
		["muckraker.libel_letter"] = "$50; suspicion −2, and you become better known: a threat is a story too.",
		["muckraker.no_reply"] = "Suspicion +5: 'did not reply' reads badly in print.",
		["reporter_returns.pay_again"] = "$100; suspicion −3. He promises it is the last time.",
		["reporter_returns.give_a_rival"] = "He writes up the rival, and the whole trade grows hotter: suspicion +3, standing −1. The rival guesses who, and for six months takes three callers in ten from any bottle dearer than 50c.",
		["reporter_returns.let_him"] = "Suspicion +12, standing −4: you are in the magazine.",
		["ama_council.submit"] = "Standing +1, suspicion +3: the Council will know what is in it. About two more callers a month for a year, on physicians' advice.",
		["ama_council.decline"] = "Standing −1: the Journal prints the names of those who declined.",
		["pa_war_chest.pay_share"] = "$150; suspicion −6.",
		["pa_war_chest.resign"] = "Standing +1, and you become a little better known.",
		["pa_war_chest.with_the_bills"] = "Suspicion +2: the Association notes who did not pay.",
		["red_clause_called.write_editors"] = "Suspicion −8, but it is the kind of letter that gets printed: standing −2, and you become better known.",
		["red_clause_called.hold_off"] = "Costs nothing.",
		["congress_member.contribute"] = "$100; suspicion −6.",
		["congress_member.for_the_bill"] = "Standing +1, and you become better known.",
		["congress_member.no_views"] = "Costs nothing.",
		["stowe_washington.go_with_her"] = "Standing +1, suspicion +4: the committee will know what is in it. You become better known.",
		["stowe_washington.donation"] = "$50; suspicion −4. She will say you were generous.",
		["stowe_washington.let_her_say"] = "Suspicion +3.",
		["label_lawyer.retain"] = "$75; suspicion −4, and you become a little better known.",
		["label_lawyer.plain_label"] = "$10 for a new plate; standing +1.",
		["label_lawyer.wait_and_see"] = "Suspicion +4.",
		["food_not_drug.become_food"] = "$30; suspicion −3, and you become better known.",
		["food_not_drug.stay_medicine"] = "Standing +1.",
		["still_smell.put_up_pipe"] = "$6; standing +1.",
		["still_smell.buy_bonnet"] = "$3 for a bonnet; she complains to her customers instead: suspicion +1.",
		["still_smell.let_her_complain"] = "Suspicion +2, standing −1.",
		["apprentice.take_him_on"] = "$4 a month for a year. He weighs every batch, and pours away a bad one before it reaches the shelf. He also sees what goes in it: suspicion +1.",
		["apprentice.no_room"] = "Costs nothing.",
		["sign_painter.gilt_sign"] = "$12, and about two more callers a month, for good.",
		["sign_painter.touch_up"] = "$2, and about one more caller a month for two years.",
		["sign_painter.leave_it"] = "Costs nothing.",
		["burglary.consideration"] = "The till held $8; you pay $4 to have it found, and are $4 down. Somebody keeps a book of who paid.",
		["burglary.bars"] = "The $8 in the till, and $3 for bars; no one comes in that way again.",
		["burglary.write_off"] = "The $8 in the till is gone.",
		["sweatshop.sell_by_gross"] = "About $30; suspicion +3, and the girls' families talk.",
		["sweatshop.refuse"] = "Costs nothing.",
		["gentleman_morphine.supply"] = "About $25; suspicion +3.",
		["gentleman_morphine.refer_him"] = "Standing +1.",
		["revenue_stamp.pay_tax"] = "$25; suspicion −2.",
		["revenue_stamp.see_deputy"] = "$8; suspicion +2, and you become a little better known.",
		["revenue_stamp.dispute"] = "Suspicion +3: a deputy will come and drink some.",
		["pushcart.hire_cart"] = "50c a month for six months, and about two more callers a month; suspicion +1.",
		["pushcart.decline"] = "Costs nothing.",
		["photographer.sit_for_it"] = "$5; standing +1, you become better known, and about one more caller a month, for good.",
		["photographer.decline"] = "Costs nothing.",
		["obituary_desk.buy_space"] = "$10; suspicion −3.",
		["obituary_desk.write_denial"] = "Suspicion +1, standing −1, and you become better known: a denial is a paragraph too.",
		["obituary_desk.let_it_print"] = "Suspicion +2, and the ward reads it.",
		["phrenologist.agree"] = "About two more callers a month for six months; standing −1, and you become a little better known.",
		["phrenologist.decline"] = "Costs nothing.",
		["school_principal.no_boys"] = "About $2 of trade lost; standing +1.",
		["school_principal.smaller_bottle"] = "About $2; suspicion +3, and the mothers hear of it.",
		["school_principal.file_it"] = "Suspicion +1, and the mothers hear of it.",
		// (end of events pass 1)
		// Events pass 2: the everyday letters (playtests/events-pass-2.md).
		["clerk_applies.engage"] = "$3 a month for two years. She checks every bill from Pearl street: every ingredient a twentieth cheaper while she stays.",
		["clerk_applies.decline"] = "Costs nothing.",
		["clerk_books.dun_him"] = "$3.40 from Hobart's, and $2 of Mr. Dunphy's four; the Alderman hears of it: suspicion +2.",
		["clerk_books.let_him_be"] = "$3.40 from Hobart's.",
		["clerk_leaves.hire_cousin"] = "$2 for a silver cruet; then $3 a month for two years, and every ingredient a twentieth cheaper while Miss Agnes stays.",
		["clerk_leaves.character_only"] = "$2 for a silver cruet; standing +1: the insurance office hears of it.",
		["lodge_invite.join"] = "$5, then 25c a month in dues, for good; about one more caller a month from the brothers.",
		["lodge_invite.decline"] = "Costs nothing.",
		["lodge_brother.slate"] = "About $3 that may come back one day; standing +1 with the lodge.",
		["lodge_brother.one_bottle"] = "50c.",
		["lodge_brother.refuse"] = "Standing −1: the lodge talks.",
		["lodge_supper.punch"] = "$4 of spirit and lemons; you become better known, and about two more callers a month for two months.",
		["lodge_supper.ticket"] = "$1.50; standing +1.",
		["lodge_supper.regrets"] = "$1.",
		["apprentice_exam.keep_him"] = "$6 a month for a year; he weighs every batch, and pours away a bad one. Standing +1 for the name on the window; he sees what goes in: suspicion +1.",
		["apprentice_exam.let_him_go"] = "$1 for the dinner.",
		["carrier_address.dollar"] = "$1; the verses make you a little better known, and the talk in the ward softens a little.",
		["carrier_address.quarter"] = "25c.",
		["carrier_address.nothing"] = "Standing −1: the boy has a route, and a mouth.",
		["holiday_goods.order_case"] = "$5, and about three more callers a month for two months, for the window.",
		["holiday_goods.decline"] = "Costs nothing.",
		["ward_turkeys.subscribe"] = "$5; suspicion −2, and you become a little better known. Your name is on the list.",
		["ward_turkeys.cough_syrup"] = "$2 in bottles; suspicion −1, standing +1.",
		["ward_turkeys.parish"] = "$1 to the parish; suspicion +1: the list is kept.",
		["lent_pledge.medicine"] = "About $1 more through Lent; suspicion +1: Mrs. Conlon shows the League.",
		["lent_pledge.it_counts"] = "About $1 of Lent's trade lost; standing +1.",
		["lent_pledge.keep_out"] = "Nothing changes; Mr. Conlon pays his dollar.",
		["easter_complexion.cold_cream"] = "20c of glycerine and rosewater; the talk in the ward softens a little.",
		["easter_complexion.wafers"] = "50c; suspicion +2.",
		["easter_complexion.lemon"] = "Costs nothing.",
		["moving_day.send_it"] = "25c a month for a year, for the boy's time at the express office; the talk in the ward softens a little.",
		["moving_day.name_druggist"] = "Costs nothing.",
		["moving_debts.hand_over"] = "About $4, after their third; standing −1, and the ward hears how it was collected.",
		["moving_debts.write_off"] = "Costs nothing more.",
		["summer_fans.order_fans"] = "$4, and about two more callers a month for three months.",
		["summer_fans.decline"] = "Costs nothing.",
		["summer_syrups.behind_counter"] = "About $2 of trade lost; standing +1, and the talk in the ward softens a little.",
		["summer_syrups.on_counter"] = "Suspicion +1, and the ward notices.",
		["fourth_of_july.two_dollars"] = "$2, and about two more callers in July.",
		["fourth_of_july.open_door"] = "$1 of lint and oil; standing +1, and the talk in the ward softens a little.",
		["fourth_of_july.shutters"] = "Standing −1.",
		["excursion.give_box"] = "$2; standing +1.",
		["excursion.programme"] = "$1.50; you become better known, and about two more callers next month. The rector tastes it: suspicion +1.",
		["excursion.regrets"] = "Standing −1.",
		["snow_sidewalk.hire_boy"] = "25c a month for three months.",
		["snow_sidewalk.lady_bottle"] = "30c; standing +1.",
		["snow_sidewalk.thaw"] = "Suspicion +1, standing −1.",
		["coal_dealer.now"] = "$4 now.",
		["coal_dealer.bucket"] = "$1 a month for five months.",
		["calendar_printer.order"] = "$6, and about one more caller a month for a year.",
		["calendar_printer.decline"] = "Costs nothing.",
		["cloudburst.pump"] = "$4.",
		["cloudburst.wait"] = "What was in the cellar spoils: every ingredient a tenth dearer for two months, and the smell: suspicion +1.",
		["school_prize.physiology"] = "$1.50; the mothers hear of it, and the talk in the ward softens a little.",
		["school_prize.penmanship"] = "$1.",
		["school_prize.regrets"] = "Standing −1.",
		["choir_lozenges.horehound"] = "50c; the talk in the ward softens a little.",
		["choir_lozenges.strongest"] = "50c; the tenors are heard on Grand Street: standing −1, you become better known, and about two more callers next month.",
		["choir_lozenges.regrets"] = "Costs nothing.",
		["target_excursion.silver_cup"] = "$4, and your name on the target: the Guard are the Leader's young men, so suspicion −1, and you become a little better known.",
		["target_excursion.case_of_tonic"] = "$2 in bottles, drunk at College Point by forty men with muskets: you become better known, suspicion +1.",
		["target_excursion.shutters"] = "$3 for the glazier on the fifteenth.",
		["labor_day.close"] = "About $1.50 of trade lost; standing +1.",
		["labor_day.lemonade"] = "About $1.50 more; standing −1.",
		["slate_credit.slate"] = "About 60c that never comes back; the talk in the ward softens a little.",
		["slate_credit.half"] = "About 30c that never comes back.",
		["slate_credit.cash"] = "Standing −1.",
		["wrong_bottle.apologise"] = "60c.",
		["wrong_bottle.poison_labels"] = "$2 for the new labels; suspicion −1.",
		["wrong_bottle.syrup_only"] = "30c; standing −1: Mr. Trask tells the street.",
		["horse_order.sell_gallon"] = "$2.50; the funeral coach smells of your tonic: you become a little better known.",
		["horse_order.veterinary"] = "Costs nothing.",
		["thanks_note.sample"] = "30c, and about one more caller a month for three months.",
		["thanks_note.thank_her"] = "Costs nothing.",
		["window_ball.nickels"] = "$2 for the glazier now; about $1 comes back in nickels, a Saturday at a time. The talk in the ward softens a little.",
		["window_ball.send_bill"] = "He pays it, somehow; standing −1.",
		["bad_note.describe"] = "The $2 is gone; suspicion −1, but your name is in the court report: you become a little better known.",
		["bad_note.stand_loss"] = "The $2 is gone.",
		["bottle_boys.washed"] = "About $2 saved on new glass.",
		["bottle_boys.unwashed"] = "About $3 saved on new glass; a customer finds something in one: suspicion +1.",
		["ship_chest.tonic"] = "About $4; suspicion +1, for whatever happens off Cape Horn.",
		["ship_chest.dispensatory"] = "About $1.50.",
		["theatre_order.coloured_water"] = "20c; you become better known, and about two more callers a month for two months.",
		["theatre_order.own_tonic"] = "60c; the actress takes it eight times a week: suspicion +1. You become better known, and about three more callers a month for two months.",
		["theatre_order.decline"] = "Costs nothing.",
		["history_book.engraving"] = "$8; standing +1, you become better known, and about one more caller a month for a year.",
		["history_book.sketch"] = "$3; standing +1, and you become a little better known.",
		["history_book.decline"] = "Costs nothing.",
		["fire_insurance.double"] = "50c a month more for a year.",
		["fire_insurance.move_barrel"] = "$1 for the cigar and the carter; the surveyor talks: suspicion +1.",
		["fire_insurance.lapse"] = "The landlord's insurers hear of it, and so does the rent: up 50c a month for six months.",
		["charity_appeal.give"] = "$1, which the Reverend will spend. Your name is on his list, which he shows the next shop: standing +1.",
		["charity_appeal.sergeant"] = "Suspicion −1; the Reverend's friends learn who sent it: you become a little better known.",
		["charity_appeal.stove"] = "Costs nothing.",
		["doctor_counter.send_cases"] = "About $2 of trade sent to the dispensary; standing +1.",
		["doctor_counter.sharp_reply"] = "Standing +1 with the ward; you become better known, and he tells the Board: suspicion +1.",
		["doctor_counter.ignore"] = "Suspicion +2.",
		["tonic_taste.free_bottle"] = "40c, and Mr. Haggerty stays.",
		["tonic_taste.taste_batches"] = "$1 a month for three months; a bad batch is poured away before it reaches the shelf.",
		["tonic_taste.say_nothing"] = "Standing −1.",
		["child_errand.fill_jug"] = "50c, on the slate; suspicion +2, and the ward sees the girl go home with the jug.",
		["child_errand.send_home"] = "Mr. McKeon takes his back across the street: about $1 of trade lost. Standing +1.",
		["carboy.lawyer"] = "The lawyer costs $1; Hobart's pay $2 to be rid of it, then add it back: every ingredient a twentieth dearer for six months. You become a little better known.",
		["carboy.pay"] = "$3.",
		["label_printer.soak_off"] = "$1 for the boy's time.",
		["label_printer.leave_them"] = "The ward reads a label after all: you become a little better known, and about two more callers come to see one.",
		["jobber_rival.trial"] = "$5, and every ingredient 8% cheaper for six months.",
		["jobber_rival.stay"] = "Costs nothing.",
		["neighbour_fire.send_all"] = "$3; standing +1, and the talk in the ward softens.",
		["neighbour_fire.at_cost"] = "About $1; standing +1.",
		["neighbour_fire.sympathy"] = "Standing −1.",
		["agent_paint.estate_green"] = "$4.",
		["agent_paint.own_colours"] = "$6, and about one more caller a month for a year.",
		["agent_paint.ignore"] = "Mr. O'Rourke passes the lawyer's letter on, with the rent: up 50c a month for a year.",
		["agent_noxious.in_writing"] = "About $1.50 a year in bottles, which the agent analyses at home.",
		["agent_noxious.let_them_sue"] = "$2 for a lawyer's letter; the ward likes a man who fights the Estate: standing +1, and you become a little better known.",
		["agent_noxious.ignore"] = "The tenant upstairs writes to the Board next: suspicion +2.",
		["boy_measles.pay_both"] = "$1; the talk in the ward softens a little.",
		["boy_measles.joseph"] = "50c.",
		["boy_measles.do_without"] = "Deliveries are late: standing −1.",
		["wedding_punch.three_gallons"] = "About $4; suspicion +2.",
		["wedding_punch.port"] = "50c; standing +1.",
		["wedding_punch.decline"] = "Costs nothing.",
		["perfume_copy.essence_of_paris"] = "About $3; you become a little better known, on Grand Street, in French.",
		["perfume_copy.plain_label"] = "About $1.50.",
		["sealer.adjust"] = "$1 in fees.",
		["sealer.look_again"] = "25c; suspicion +1.",
		["sealer.dispute"] = "Suspicion +2.",
		["night_bell.put_in_bell"] = "$2 for the bell; the talk in the ward softens a little, and about one more caller a month for a year.",
		["night_bell.shop_hours"] = "Costs nothing.",
		["jury_duty.clerk"] = "$1; suspicion +1: somebody keeps a list.",
		["jury_duty.serve"] = "About $2 of trade lost; standing +1.",
		["pedestal_fund.box_and_dollar"] = "$1; standing +1, and about one more caller a month for two months, with a cent for the box.",
		["pedestal_fund.box_and_dime"] = "10c, and about one more caller a month for two months.",
		["pedestal_fund.decline"] = "Costs nothing.",
		["centennial_1889.dress_shop"] = "$2.80; standing +1.",
		["centennial_1889.washington"] = "20c for the print; you become a little better known, and about two more callers in May.",
		["centennial_1889.nothing"] = "Standing −1.",
		["trade_cards.order_cards"] = "$3, and about two more callers a month for three months.",
		["trade_cards.decline"] = "Costs nothing.",
		["census_1890.truthfully"] = "Standing +1; suspicion +1, as somebody reads confidential returns.",
		["census_1890.chemist"] = "Nothing changes; nobody checks.",
		["columbus_1892.lanterns"] = "$1; standing +1.",
		["columbus_1892.march"] = "About $2 of trade lost; standing +1, you become a little better known, and the talk in the ward softens a little.",
		["columbus_1892.own_lamp"] = "Costs nothing.",
		["fair_medal_1893.buy_medal"] = "$8; you become a little better known, and about two more callers a month for six months.",
		["fair_medal_1893.decline"] = "Costs nothing.",
		["phonograph.record"] = "$3 a month for six months, and about three more callers a month.",
		["phonograph.decline"] = "Costs nothing.",
		["settlement_nurse.tell_her"] = "Standing +1; suspicion +1: the nurse writes it down.",
		["settlement_nurse.small_bottle"] = "About $1 of trade lost; the talk in the ward softens a little.",
		["settlement_nurse.trade_secret"] = "The nurse tells the street what she suspects.",
		["antitoxin_1895.send_them"] = "About $2 of trade lost; standing +1, and the talk in the ward softens a little.",
		["antitoxin_1895.keep_card"] = "About $2 more; suspicion +3.",
		["white_wings.three_barrels"] = "$1.50; standing +1.",
		["white_wings.tip_cartman"] = "25c; suspicion +1.",
		["white_wings.leave_it"] = "Suspicion +2.",
		["bicycle_boy.store_wheel"] = "$8, and about one more caller a month for two years.",
		["bicycle_boy.canal_street"] = "$5, and about one more caller a month for two years; its owner comes looking: suspicion +1.",
		["bicycle_boy.walk"] = "Costs nothing.",
		["x_rays_1896.accept"] = "About four more callers next month; you become a little better known, and the Board asks what the apparatus does: suspicion +1.",
		["x_rays_1896.decline"] = "Costs nothing.",
		["gold_clause_1896.sign_gold"] = "Standing −1: the ward is for silver, and hears you signed for gold.",
		["gold_clause_1896.silver_card"] = "Every ingredient a tenth dearer for four months; standing +1, and you become a little better known.",
		["gold_clause_1896.say_nothing"] = "Every ingredient a tenth dearer for four months.",
		["raines_law.half_shares"] = "About $2.50 over the summer; suspicion +2, and you become a little better known.",
		["raines_law.decline"] = "Costs nothing.",
		["school_inspector.at_cost"] = "50c; the talk in the ward softens a little.",
		["school_inspector.nursery_lotion"] = "About $3; standing −1.",
		["school_inspector.usual_profit"] = "About $1.",
		["consolidation_1898.pay_dollar"] = "$1.",
		["consolidation_1898.district_leader"] = "$2; suspicion −2. Your name is on another list.",
		["consolidation_1898.wait"] = "Suspicion +2.",
		["war_stamps_1898.stamp_all"] = "About 50c a month in stamps, for two years.",
		["war_stamps_1898.steam_them"] = "About 15c a month for two years; suspicion +3.",
		["war_stamps_1898.no_stamps"] = "Suspicion +4.",
		["telephone.install"] = "$10, then $1 a month; about two more callers a month, for good.",
		["telephone.decline"] = "Costs nothing.",
		["liquid_air.testimonial"] = "$3; your name in a prospectus that the District Attorney reads: suspicion +2, and you become a little better known.",
		["liquid_air.decline"] = "Costs nothing.",
		["subway_digging.plank"] = "$2.",
		["subway_digging.wait"] = "About $1 of trade lost; customers think the shop is shut: standing −1.",
		["smallpox_1901.post_notice"] = "It frightens a few customers: about 50c lost. Standing +1, and the talk in the ward softens a little.",
		["smallpox_1901.preventive"] = "About $4; suspicion +4.",
		["smallpox_1901.neither"] = "Suspicion +1.",
		["trading_stamps.give_stamps"] = "$2 a month for a year, and about two more callers a month.",
		["trading_stamps.decline"] = "Costs nothing.",
		["motorist.benzine"] = "$2; your gallons of benzine are talked of at the insurance office: suspicion +1.",
		["motorist.for_the_road"] = "$2.60; suspicion +1, you become a little better known, and about one more caller a month for three months, in goggles.",
		["motorist.send_for_horse"] = "Costs nothing.",
		["postcards.cards"] = "$5; you become a little better known, and about two more callers a month for six months.",
		["postcards.decline"] = "Costs nothing.",
		["electric_sign.install"] = "$20, then $1 a month; about two more callers a month, for good.",
		["electric_sign.decline"] = "Costs nothing.",
		["nickelodeon.slide"] = "$1 a month for six months, and about three more callers a month.",
		["nickelodeon.decline"] = "Costs nothing.",
		// (end of events pass 2)
		// the letters fix of round 8
		["burglary_book.subscribe"] = "$5; suspicion −3. Your name stays in the book.",
		["burglary_book.out_of_the_book"] = "$2, and your name comes out of this book; the sergeant tells the station-house why: suspicion +1.",
		["burglary_book.for_the_till"] = "Suspicion +2: the Captain does not forget, either way.",
		["thanks_sister.another"] = "30c, and about one more caller a month for four months: Mr. Garrity tells his street it does nothing.",
		["thanks_sister.sell_one"] = "About 50c.",
		["thanks_sister.believe_him"] = "The talk in the ward softens a little: an honest druggist is a curiosity.",
		["bad_note_trial.attend"] = "About $1 of trade lost, net of the fee; suspicion −1, and your name is in the court report: you become a little better known.",
		["bad_note_trial.not_sure"] = "Suspicion +1: the Service notes a witness who forgets.",
		["carrier_address_new.dollar"] = "$1; the verses make you a little better known, and the talk in the ward softens a little.",
		["carrier_address_new.quarter"] = "25c.",
		["carrier_address_new.nothing"] = "Standing −1: the boy has a route, and a mouth.",
		// (end of the letters fix)
	};

	/// <summary>Where a printed letter is dated from, when it is not New-York.</summary>
	private static readonly Dictionary<string, string> Places = new()
	{
		["diploma_mill"] = "Cincinnati",
		["medicine_show_offer"] = "Elmira",
		["medicine_show_returns"] = "Elmira",
		["state_chemist"] = "Albany",
		["army_contract"] = "Washington",
		["army_inquiry"] = "Washington",
		["bureau_sample"] = "Washington",
		["radium_water"] = "Newark",
		["ama_council"] = "Chicago",
		["congress_member"] = "Washington",
		// pass 2 places
		["fair_medal_1893"] = "Chicago",
		// (end of pass 2 places)
	};

	private static string Tenths(int n) => n switch { 1 => "a tenth", 2 => "a fifth", 3 => "three-tenths", 4 => "two-fifths", 5 => "half", 6 => "three-fifths", 7 => "seven-tenths", 8 => "four-fifths", _ => "nine-tenths" };

	/// <summary>"one in 20", "three in 10": odds a reader can feel.</summary>
	private static string Chance(double p) =>
		p < 0.05 ? "fewer than one in 20" : p < 0.1 ? "about one in 10" : $"about {Math.Clamp((int)Math.Round(p * 10), 1, 9)} in 10";

	public LettersDesk(IGame game)
	{
		_game = game;
		AddThemeConstantOverride("separation", 14);
		Refresh();
	}

	public void Refresh()
	{
		var keep = Print.FocusIndex(this);
		RefreshBody();
		Print.RestoreFocus(this, keep);
	}

	private void RefreshBody()
	{
		foreach (var c in GetChildren()) { RemoveChild(c); c.QueueFree(); }
		var d = _game.Desk();
		_heat = d.Heat;
		var top = new HBoxContainer();
		top.AddChild(Print.Label("THE MORNING'S POST", Print.Rye, 22, color: Print.Paper));
		top.AddChild(Print.Label(d.Letters.Count == 0 ? "nothing for you" : d.Letters.Count == 1 ? "one letter" : $"{d.Letters.Count} letters", Print.FellIt, 17, HorizontalAlignment.Right, Print.Paper));
		// a band of ink, so the heading never sits on the proof behind the shade
		AddChild(Print.Panel(top, Print.Box(Print.Ink, Print.Ink, 0, 8)));
		if (_note != null) AddChild(Print.Slip(Print.Body($"[i]{Print.Escape(EditionWriter.Set(_note))}[/i]", 15, justify: false), 10));
		if (d.Letters.Count == 0)
		{
			AddChild(Envelope());
			return;
		}
		// each letter keeps the date it arrived: fixed by the letter itself, never after the proof
		var press = EditionWriter.PublicationDate(d.Year, d.Month);
		for (var i = 0; i < d.Letters.Count; i++)
		{
			var h = 0u;
			foreach (var ch in d.Letters[i].InstanceId) h = (h ^ ch) * 16777619u;
			AddChild(Sheet(d.Letters[i], press.AddDays(-2 - (int)(h % 12)), i, Served(d.Turn) + d.Letters.Take(i + 1).Count(x => x.EventId == "inquest")));
		}
	}

	private Control Envelope()
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 4);
		v.AddChild(Print.Label("A CIRCULAR", Print.Rye, 18, HorizontalAlignment.Center));
		v.AddChild(Print.Body("[center][i]Dr. Grimsby begs to announce his Galvanic Belt, which restores Lost Vigor through every Nerve. Sent on thirty days' trial.[/i][/center]", 15, justify: false));
		v.AddChild(Print.Body("[center]Nothing else in the post this morning.[/center]", 14, justify: false));
		var s = Print.Slip(v, 18);
		s.RotationDegrees = -1f;
		return s;
	}

	/// <summary>Inquest notices answered so far this month (the month's turn, and the count).</summary>
	private static (int Turn, int Count) _served;
	private static int Served(int turn) => _served.Turn == turn ? _served.Count : 0;

	private Control Sheet(Letter l, DateTime date, int index, int inquestNo = 0)
	{
		var head = Heads.GetValueOrDefault(l.EventId) ?? new Head("", "", "Yours, &c.", "", "", "Sir,—", "");
		var at = l.Title.IndexOf("Death of ", StringComparison.Ordinal);
		var concerns = at >= 0 ? l.Title[(at + 9)..].Replace("the Infant of", "the infant child of").Replace("an Unknown", "an unknown") : "";
		if (concerns.StartsWith("an unknown", StringComparison.Ordinal)) concerns = concerns.ToLowerInvariant();
		var body = head.Voice.Length > 0 ? head.Voice.Replace("{name}", concerns.Length > 0 ? concerns : "a person") : l.Body;
		// the Clerk keeps count: a busy month's summonses say so
		if (l.EventId == "inquest" && inquestNo >= 2)
			body += $" This is the {Nth(inquestNo)} such notice the Clerk has had the honour to serve on you this month.";
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 6);

		// the clerk's docket: how the letter came
		if (head.Docket.Length > 0) v.AddChild(Print.Label(head.Docket, Print.FellIt, 13, color: new Color(Print.Ink, 0.85f)));

		// the letterhead: printed, or nothing (a hand-written note)
		var lh = new HBoxContainer();
		lh.AddThemeConstantOverride("separation", 14);
		var names = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		names.AddThemeConstantOverride("separation", 0);
		if (head.Sender.Length > 0)
		{
			names.AddChild(Print.Label(head.Sender, Print.Rye, 17));
			names.AddChild(Print.Label(head.Address, Print.FellIt, 13));
		}
		else names.AddChild(Print.Label(head.Address, Print.FellIt, 15));
		lh.AddChild(names);
		if (head.Seal.Length > 0) lh.AddChild(Seal(head.Seal));
		v.AddChild(lh);
		if (head.Sender.Length > 0) v.AddChild(Print.Rule(2, 0.8f, 2));
		// a printed letter is dated; a note pushed under the door knows only the day
		var note = head.Sender.Length == 0;
		v.AddChild(Print.Label(note ? date.DayOfWeek + "." : $"{Places.GetValueOrDefault(l.EventId, "New-York")}, {date.ToString("MMMM d", CultureInfo.InvariantCulture)}{Ordinal(date.Day)}, {date.Year}.", Print.FellIt, 14, HorizontalAlignment.Right));
		v.AddChild(Print.Label("Re: " + EditionWriter.Set(l.Title), Print.Rye, 15));
		v.AddChild(Print.Body(note ? $"[i]{Print.Escape(EditionWriter.Set(head.Salute + body))}[/i]" : Print.Escape(EditionWriter.Set(head.Salute + body)), note ? 17 : 16, justify: false));
		v.AddChild(Print.Label(l.From != null ? $"Your grateful servant, {l.From}" : head.Sign, Print.FellIt, 14, HorizontalAlignment.Right));

		// the replies, each a slip to send back
		v.AddChild(Print.Label("YOUR REPLY", Print.Fell, 12));
		Control? sheet = null;
		foreach (var (id, text) in l.Choices)
		{
			var b = Print.Button("☞  " + EditionWriter.Set(text), 14, sound: null);
			b.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			b.Alignment = HorizontalAlignment.Left;
			var choice = id;
			var said = text;
			b.Pressed += () => Answer(sheet!, l.InstanceId, choice, said);
			v.AddChild(b);
			// the reply and the line under it come into view together, with room to spare
			Control? riskLine = null;
			b.FocusEntered += () => Callable.From(() =>
			{
				for (Node? n = b; n != null; n = n.GetParent())
					if (n is ScrollContainer sc)
					{
						sc.EnsureControlVisible(riskLine ?? b);
						var bottom = (riskLine ?? b).GetGlobalRect().End.Y - sc.GetGlobalRect().End.Y;
						if (bottom > -30) sc.ScrollVertical += (int)(bottom + 30);
						break;
					}
			}).CallDeferred();
			if (Risks.TryGetValue($"{l.EventId}.{id}", out var risk))
			{
				string? pencil = null;
				// the verdict is given as soon as the reply goes: say what the odds are now
				if (l.Odds.TryGetValue(id, out var p))
					pencil = id == "bribe_coroner"
						? $"Pencilled in the margin: {Chance(p)} that it comes out. —J.D."
						: $"Pencilled in the margin: {Chance(p)} for naming" +
							(l.Odds.TryGetValue(id == "testify" ? "stay_silent" : "testify", out var q) && Chance(q) == Chance(p) ? ", speak or not" : "") + ". The jury sits at once. —J.D.";
				var line = Print.Label(risk, Print.FellIt, 15, color: Print.Ink);
				riskLine = line;
				v.AddChild(line);
				if (pencil != null)
				{
					var margin = Print.Label(pencil, Print.FellIt, 14, HorizontalAlignment.Right, Print.Ink);
					margin.RotationDegrees = -0.8f;
					riskLine = margin;
					v.AddChild(margin);
				}
			}
		}
		// a suspected shop is taken at its word less: the standing a kindness earns, and the callers a
		// correspondent sends, are scaled down (the sim's letterTrust, with config.scrutiny.letterCallerLoss
		// 1.5; the ward's memory lowers it further)
		if (_heat >= 14 && l.EventId != "inquest")
		{
			bool Says(string what) => l.Choices.Any(c => Risks.TryGetValue($"{l.EventId}.{c.Id}", out var r) && r.Contains(what, StringComparison.OrdinalIgnoreCase));
			bool standing = Says("standing +"), callers = Says("more caller") || Says("callers a month come") || Says("callers come");
			var tenths = Math.Clamp((int)Math.Round(Math.Max(0, 1 - 1.5 * _heat / 100) * 10), 0, 9);
			var share = Tenths(tenths);
			var credit = tenths == 0 ? "the ward credits none of the standing a reply says" : $"the ward credits at most {share} of the standing a reply says";
			var come = tenths == 0 ? "nobody comes on a letter's word" : $"at most {share} as many come on a letter's word";
			var said = standing && callers ? $"{credit}, and {come}" : standing ? credit : callers ? come : null;
			if (said != null)
			{
				var wary = Print.Label($"In your own hand, in the margin: with the shop suspected as it is, {said}.", Print.FellIt, 14, HorizontalAlignment.Right, Print.Ink);
				wary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
				v.AddChild(wary);
			}
		}
		var s = Print.Slip(v, 18);
		// notes lie crooked on the desk; printed letters nearly straight
		s.RotationDegrees = (index % 2 == 0 ? -0.6f : 0.5f) * (head.Sender.Length == 0 ? 3f : 1f);
		sheet = s;
		return s;
	}

	private void Answer(Control sheet, string instance, string choice, string said)
	{
		var d0 = _game.Desk();
		var before = d0.CashCents;
		var heat0 = d0.Heat;
		var inquest = d0.Letters.FirstOrDefault(x => x.InstanceId == instance)?.EventId == "inquest";
		var err = _game.Resolve(instance, choice);
		// the Clerk counts the notices he has served this month, answered ones included
		if (inquest && err == null) _served = (d0.Turn, Served(d0.Turn) + 1);
		if (err != null) { _note = "Refused: " + err; Refresh(); return; }
		var d1 = _game.Desk();
		var after = d1.CashCents;
		var money = after == before ? "" : after < before ? $" It cost {EditionWriter.Money(before - after)}." : $" It brought in {EditionWriter.Money(after - before)}.";
		// say what happened at once; only what has not happened yet is left to the paper
		var stockGone = d0.Formulas.Sum(f => f.Stock) - d1.Formulas.Sum(f => f.Stock);
		var talk = d1.Heat - heat0;
		var standing = d1.Reputation - d0.Reputation;
		var now = new List<string>();
		if (stockGone > 0) now.Add($"{stockGone} bottles are gone from the shelf");
		if (Math.Abs(talk) >= 1) now.Add($"suspicion {(talk > 0 ? "rises" : "falls")} {Math.Abs(talk):0}");
		if (Math.Abs(standing) >= 1) now.Add($"your standing {(standing > 0 ? "rises" : "falls")} {Math.Abs(standing):0}");
		var outcome = !inquest ? (now.Count > 0 ? " At once: " + string.Join("; ", now) + "." : "") + " Anything further will be in the next edition."
			// a bribe can come out at once: then say so, not that the verdict was bought cleanly
			: choice == "bribe_coroner" && d1.Heat > heat0 + 10 ? $" The coroner took your envelope, and the ward heard of it before the jury sat. Suspicion is now {d1.Heat:0} of 100{(standing <= -1 ? $", and your standing falls {Math.Abs(standing):0}" : "")}. The paper will have the rest."
			: choice == "bribe_coroner" ? " The coroner has your envelope; the verdict will be the one you paid for."
			: d1.Heat > heat0 + 10 ? $" The jury sat at once and named your bottle. Suspicion is now {d1.Heat:0} of 100. The paper will have the rest."
			: " The jury sat at once and found nothing against you. The paper will have the verdict.";
		_note = $"Sent: “{said}”{money}{outcome}";
		Main.Guard(800);

		// the rubber stamp comes down, and the letter is folded away
		Sfx.Play("sheet", 0f, 0.05f, 0.8f);
		var stamp = Print.Panel(Print.Label("ANSWERED", Print.Rye, 30, HorizontalAlignment.Center, new Color(Print.Ink, 0.75f), wrap: false), Print.Box(new Color(0, 0, 0, 0), new Color(Print.Ink, 0.75f), 4, 6));
		stamp.MouseFilter = MouseFilterEnum.Ignore;
		stamp.Position = new Vector2(sheet.Size.X * 0.5f - 110, 30);
		stamp.RotationDegrees = -14;
		stamp.Scale = new Vector2(1.6f, 1.6f);
		sheet.AddChild(stamp);
		stamp.TopLevel = false;
		var t = sheet.CreateTween();
		t.TweenProperty(stamp, "scale", Vector2.One, 0.12f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		t.TweenInterval(0.45f);
		t.TweenCallback(Callable.From(() => { sheet.PivotOffset = new Vector2(sheet.Size.X / 2, 0); Sfx.Play("slip", -4f); }));
		t.TweenProperty(sheet, "scale:y", 0.05f, 0.25f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		t.Parallel().TweenProperty(sheet, "modulate:a", 0f, 0.25f);
		t.TweenCallback(Callable.From(() =>
		{
			Refresh();
			Changed?.Invoke();
			// the next letter's first reply takes the keyboard, scrolled into view
			Callable.From(() =>
			{
				if (!IsInsideTree()) return;
				var first = Print.Focusables(this).OfType<Button>().FirstOrDefault(x => !x.Disabled);
				if (first == null) return;
				first.GrabFocus();
				for (Node? n = this; n != null; n = n.GetParent())
					if (n is ScrollContainer sc) { sc.EnsureControlVisible(first); break; }
			}).CallDeferred();
		}));
	}

	/// <summary>
	/// The sender's engraved seal (tools/make_seals.py): a roundel with its legend round the rim,
	/// printed in the letterhead's corner.
	/// </summary>
	public static Control Seal(string cut, int size = 104)
	{
		var tex = Print.Cut(cut);
		var r = new TextureRect
		{
			Texture = tex,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			CustomMinimumSize = new Vector2(size, size),
			SizeFlagsVertical = SizeFlags.ShrinkBegin,
			PivotOffset = new Vector2(size / 2f, size / 2f),
			RotationDegrees = -8,
		};
		return r;
	}

	private static string Nth(int n) => n switch { 2 => "second", 3 => "third", 4 => "fourth", 5 => "fifth", 6 => "sixth", 7 => "seventh", 8 => "eighth", _ => $"{n}th" };

	private static string Ordinal(int d) => (d % 100) is 11 or 12 or 13 ? "th" : (d % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
}
